using System.Net;

using Collectibles.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Web.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    private readonly IClientIpResolver _clientIpResolver;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IServiceProvider serviceProvider,
        IClientIpResolver clientIpResolver)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _clientIpResolver = clientIpResolver;
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

        // A concurrency conflict is a normal outcome now that the editable aggregates carry
        // row versions: someone else saved first. Report it as a conflict the caller can
        // recover from by reloading, not as a server fault.
        if (exception is DbUpdateConcurrencyException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(
                "This record was changed by someone else while you were editing it. Reload and try again.");
            return;
        }

        if (exception is UnauthorizedAccessException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("You are not authorized to perform this action.");
            return;
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

    private string? GetIPAddress(HttpContext context)
    {
        // Resolved centrally: parsing forwarded headers here trusted whatever the client
        // sent and wrote a forged address into the audit trail.
        return _clientIpResolver.Resolve(context);
    }

    private static bool IsDevelopment()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return environment == "Development";
    }
}
