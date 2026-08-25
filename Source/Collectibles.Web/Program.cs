using Collectibles.Domain.Constants;
using Collectibles.Infrastructure.Logging;
using Collectibles.Infrastructure.Persistence;
using Collectibles.Web.Components;
using Collectibles.Web.Extensions;
using Collectibles.Web.Middleware;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

SerilogThemeExtensions.ConfigureEarlySerilogLogging();

try
{
    Log.Information("Starting Collectibles Web Application");

    // Configure thread pool to prevent starvation during high load
    ThreadPool.SetMinThreads(ApplicationConstants.RequestLimits.MinThreadPoolThreads, ApplicationConstants.RequestLimits.MinThreadPoolThreads);

    var builder = WebApplication.CreateBuilder(args);

    if (builder.Environment.IsEnvironment("Playwright"))
    {
        // Enable static web assets when running the app locally under the Playwright environment.
        builder.WebHost.UseStaticWebAssets();
    }

    // Configure IIS Integration
    builder.WebHost.UseIISIntegration();

    // Configure Kestrel for large file uploads (20GB)
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.Limits.MaxRequestBodySize = ApplicationConstants.RequestLimits.MaxRequestBodySize;
        serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(ApplicationConstants.Web.RequestHeadersTimeoutMinutes);
        serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(ApplicationConstants.Web.KeepAliveTimeoutMinutes);
    });


    // Use Serilog for all logging
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        var logConfig = configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            // Filter out MediatR license messages
            .Filter.ByExcluding(logEvent =>
                logEvent.MessageTemplate.Text.Contains("Lucky Penny") ||
                logEvent.MessageTemplate.Text.Contains("valid license key") ||
                logEvent.MessageTemplate.Text.Contains("luckypennysoftware"))
            .WriteTo.Console(theme: SerilogThemeExtensions.GetPowerShellTheme())
            .WriteTo.File(
                path: Path.Combine(ApplicationConstants.Logging.LogDirectory, ApplicationConstants.Logging.MainLogFilePattern),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: ApplicationConstants.TimeOperations.LogFileRetentionDays,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(ApplicationConstants.Logging.LogDirectory, ApplicationConstants.Logging.ErrorLogFilePattern),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: ApplicationConstants.TimeOperations.LogFileRetentionDays,
                restrictedToMinimumLevel: LogEventLevel.Error,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}");

        // Add database logging if enabled
        var enableDbLogging = context.Configuration.GetValue<bool>("Serilog:EnableDatabaseLogging", false);
        if (enableDbLogging)
        {
            var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(connectionString))
            {
                // Use the provided services directly
                // The DatabaseLoggerSink will handle cases where the database isn't ready yet
                logConfig.WriteTo.Database(
                    services,
                    connectionString,
                    restrictedToMinimumLevel: LogEventLevel.Warning);
            }
        }
    });

    // Configure all services using the extension method
    builder.Services.ConfigureServices(builder.Configuration);

    // Configure security scan blocking options from configuration
    builder.Services.Configure<SecurityScanBlockingOptions>(
        builder.Configuration.GetSection("SecurityScanBlocking"));
    builder.Services.AddSingleton<SecurityScanBlockingOptions>(provider =>
    {
        var config = builder.Configuration.GetSection("SecurityScanBlocking").Get<SecurityScanBlockingOptions>()
            ?? new SecurityScanBlockingOptions();
        return config;
    });

    // Configure crawler blocking options from configuration
    builder.Services.Configure<CrawlerBlockingOptions>(
        builder.Configuration.GetSection("CrawlerBlocking"));
    builder.Services.AddSingleton<CrawlerBlockingOptions>(provider =>
    {
        var config = builder.Configuration.GetSection("CrawlerBlocking").Get<CrawlerBlockingOptions>()
            ?? new CrawlerBlockingOptions();
        return config;
    });

    var app = builder.Build();

    // Apply database migrations before starting the application.
    // Skipped when the host is being built by the EF Core CLI (update-database / migrations),
    // which runs migrations itself and would otherwise abort the host during Build().
    var isEfCoreCli = args.Any(a => string.Equals(a, "ef", StringComparison.OrdinalIgnoreCase))
        || args.Any(a => string.Equals(a, "database", StringComparison.OrdinalIgnoreCase) && args.Contains("update", StringComparer.OrdinalIgnoreCase));
    if (isEfCoreCli)
    {
        Log.Information("EF Core CLI context detected - skipping startup migrations");
    }
    else
    {
        Log.Information("Applying database migrations...");
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                // Prefer factory-based context creation to avoid resolving scoped services from the root provider in .NET 9/EF Core 9
                var dbContextFactory = scope.ServiceProvider.GetService<IDbContextFactory<ApplicationDbContext>>();
                if (dbContextFactory is not null)
                {
                    await using var dbFromFactory = dbContextFactory.CreateDbContext();
                    await dbFromFactory.Database.MigrateAsync();
                }
                else
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await dbContext.Database.MigrateAsync();
                }

                Log.Information("Database migrations applied successfully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to apply database migrations");
                throw;
            }
        }
    }

    // Use Serilog request logging for better HTTP request/response logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.GetLevel = (httpContext, elapsed, ex) => ex != null
            ? LogEventLevel.Error
            : httpContext.Response.StatusCode >= 400
                ? LogEventLevel.Warning
                : LogEventLevel.Information;
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        };
    });

    Log.Information("Application built successfully. Configuring middleware pipeline...");

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        Log.Information("Development environment detected - enabling developer exception page");
        app.UseDeveloperExceptionPage();
        app.UseMigrationsEndPoint();

        // Add MiniProfiler middleware for performance profiling
        Log.Information("Enabling MiniProfiler for performance profiling");
        app.UseMiniProfiler();
    }
    else
    {
        Log.Information("Production environment detected - configuring production error handling");
        app.UseExceptionHandler("/Error", createScopeForErrors: true);

        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    // Add security scan blocking middleware early in the pipeline
    var securityOptions = app.Services.GetRequiredService<SecurityScanBlockingOptions>();
    if (securityOptions.Enabled)
    {
        Log.Information("Security scan blocking middleware is enabled");
        app.UseMiddleware<SecurityScanBlockingMiddleware>();
    }

    // Add crawler blocking middleware early in the pipeline
    var crawlerOptions = app.Services.GetRequiredService<CrawlerBlockingOptions>();
    if (crawlerOptions.Enabled)
    {
        Log.Information("Crawler blocking middleware is enabled");
        app.UseMiddleware<CrawlerBlockingMiddleware>();
    }

    // Add custom exception handling middleware for logging
    app.UseMiddleware<Collectibles.Web.Middleware.ExceptionHandlingMiddleware>();

    // Add request logging middleware
    app.UseMiddleware<Collectibles.Web.Middleware.RequestLoggingMiddleware>();

    app.UseHttpsRedirection();

    // Add standard security headers
    app.Use(async (context, next) =>
    {
        var headers = context.Response.Headers;

        // Prevent MIME type sniffing
        headers["X-Content-Type-Options"] = "nosniff";

        // Clickjacking protection (also enforced by CSP frame-ancestors)
        headers["X-Frame-Options"] = "DENY";

        // Referrer policy to reduce information leakage
        headers["Referrer-Policy"] = "no-referrer";

        // Restrict powerful browser features by default
        headers["Permissions-Policy"] =
            "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";

        // Content Security Policy: tuned for Blazor Server + SignalR
        // Allow self for most, images/fonts from self and data/blob, WebSockets to self
        // Google Fonts needed by Bootswatch and custom themes (via @import in theme CSS)
        var csp = string.Join("; ", new[]
        {
            "default-src 'self'",
            "base-uri 'self'",
            "object-src 'none'",
            "frame-ancestors 'none'",
            "img-src 'self' data: blob:",
            "font-src 'self' data: https://fonts.gstatic.com",
            "script-src 'self'",
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",
            "connect-src 'self' ws: wss: https://*.blob.core.windows.net",
        });

        headers["Content-Security-Policy"] = csp;

        await next();
    });

    // Add HTTP context capture middleware for Interactive Server components
    app.UseMiddleware<Collectibles.Web.Middleware.HttpContextCaptureMiddleware>();

    // Add tracking cookie middleware for lightweight session tracking
    app.UseMiddleware<Collectibles.Web.Middleware.TrackingCookieMiddleware>();

    // Add status code pages middleware to handle various HTTP errors
    app.UseStatusCodePagesWithReExecute("/error/{0}");

    app.UseStaticFiles();

    // CRITICAL: Add authentication and authorization middleware
    // This must come before UseAntiforgery and endpoint mapping
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseAntiforgery();

    // Map Blazor framework and package static assets for interactive pages.
    app.MapStaticAssets();

    // Map health check endpoints for monitoring
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds,
                }),
                totalDuration = report.TotalDuration.TotalMilliseconds,
            });
            await context.Response.WriteAsync(result);
        },
    });

    // Configure Hangfire dashboard and recurring jobs
    await app.UseHangfireAsync(builder.Configuration);

    // Map all API endpoints using the structured approach
    app.MapApiEndpoints();

    Log.Information("Mapping Razor components...");
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    // Add additional endpoints required by the Identity /Account Razor components.
    app.MapAdditionalIdentityEndpoints();

    Log.Information("Application startup complete. Starting web server...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
