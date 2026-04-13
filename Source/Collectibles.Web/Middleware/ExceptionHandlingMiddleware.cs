using System.Net;
using Collectibles.Application.Interfaces;

namespace Collectibles.Web.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred during request processing");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var sysLogService = scope.ServiceProvider.GetRequiredService<ISysLogService>();

            var properties = new Dictionary<string, object>
            {
                ["TraceIdentifier"] = context.TraceIdentifier,
                ["RequestId"] = context.Request.Headers["X-Request-ID"].FirstOrDefault() ?? string.Empty,
                ["UserAgent"] = context.Request.Headers["User-Agent"].ToString(),
                ["IPAddress"] = GetIPAddress(context) ?? "Unknown",
                ["RequestPath"] = context.Request.Path.ToString(),
                ["RequestMethod"] = context.Request.Method,
                ["QueryString"] = context.Request.QueryString.ToString(),
            };

            await sysLogService.LogCriticalAsync(
                $"Unhandled exception in {context.Request.Method} {context.Request.Path}",
                exception,
                "ExceptionHandler",
                properties);
        }
        catch (Exception loggingException)
        {
            _logger.LogError(loggingException, "Error occurred while logging exception to SysLog");
        }

        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "text/plain";

        if (IsDevelopment())
        {
            await context.Response.WriteAsync($"An error occurred: {exception.Message}\n\n{exception.StackTrace}");
        }
        else
        {
            await context.Response.WriteAsync("An error occurred while processing your request.");
        }
    }

    private static string? GetIPAddress(HttpContext context)
    {
        var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(ipAddress))
        {
            var addresses = ipAddress.Split(',');
            if (addresses.Length > 0)
            {
                return addresses[0].Trim();
            }
        }

        ipAddress = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(ipAddress))
        {
            return ipAddress;
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private static bool IsDevelopment()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return environment == "Development";
    }
}
