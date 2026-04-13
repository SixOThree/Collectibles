using Collectibles.Application.Interfaces;
using Collectibles.Infrastructure.Services;
using Collectibles.Infrastructure.Services.Email;
using Collectibles.Web.Authorization;

using Hangfire;
using Hangfire.Dashboard;

namespace Collectibles.Web.Extensions;

/// <summary>
/// Extension methods for configuring Hangfire middleware and recurring jobs.
/// </summary>
public static class HangfireExtensions
{
    /// <summary>
    /// Configures Hangfire dashboard middleware.
    /// </summary>
    /// <returns></returns>
    public static IApplicationBuilder UseHangfireDashboardWithAuth(this IApplicationBuilder app, IConfiguration configuration)
    {
        var dashboardPath = configuration["Hangfire:DashboardPath"] ?? "/hangfire";
        var requireAuthorization = configuration.GetValue<bool>("Hangfire:RequireAuthorization");

        var dashboardOptions = new DashboardOptions
        {
            Authorization = requireAuthorization
                ? new[] { new HangfireAuthorizationFilter() }
                : Array.Empty<IDashboardAuthorizationFilter>(),
        };

        app.UseHangfireDashboard(dashboardPath, dashboardOptions);

        return app;
    }

    /// <summary>
    /// Configures all Hangfire recurring jobs.
    /// </summary>
    /// <returns></returns>
    public static IApplicationBuilder ConfigureHangfireRecurringJobs(this IApplicationBuilder app, IConfiguration configuration)
    {
        var logger = app.ApplicationServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Configuring Hangfire recurring jobs...");

        try
        {
            // Email processing jobs
            ConfigureEmailJobs();

            // Cleanup jobs
            ConfigureCleanupJobs(configuration);

            // Zip upload jobs
            ConfigureZipUploadJobs();

            // Attachment indexing jobs
            ConfigureAttachmentIndexingJobs();

            // Attachment preview generation jobs
            ConfigureAttachmentPreviewJobs();

            logger.LogInformation("Hangfire recurring jobs configured successfully");
        }
        catch (Exception ex)
        {
            // Log the error but don't throw - allow the app to start even if Hangfire configuration fails
            logger.LogError(ex, "Failed to configure Hangfire recurring jobs. Background jobs may not be available.");
        }

        return app;
    }

    /// <summary>
    /// Configures email-related recurring jobs.
    /// </summary>
    private static void ConfigureEmailJobs()
    {
        RecurringJob.AddOrUpdate<EmailBackgroundService>(
            "process-pending-emails",
            service => service.ProcessPendingEmailsAsync(),
            "*/1 * * * *"); // Every minute

        RecurringJob.AddOrUpdate<EmailBackgroundService>(
            "cleanup-old-email-logs",
            service => service.CleanupOldEmailLogsAsync(),
            "0 2 * * *"); // Daily at 2 AM
    }

    /// <summary>
    /// Configures cleanup-related recurring jobs.
    /// </summary>
    private static void ConfigureCleanupJobs(IConfiguration configuration)
    {
        var requestLogRetentionDays = configuration.GetValue<int>("Logging:RequestLogRetentionDays", 365);
        if (requestLogRetentionDays <= 0)
        {
            requestLogRetentionDays = 365;
        }

        RecurringJob.AddOrUpdate<IRequestLogService>(
            "cleanup-old-request-logs",
            service => service.CleanupOldLogsAsync(requestLogRetentionDays, CancellationToken.None),
            "0 3 * * *"); // Daily at 3 AM
    }

    /// <summary>
    /// Configures zip upload-related recurring jobs.
    /// </summary>
    private static void ConfigureZipUploadJobs()
    {
        RecurringJob.AddOrUpdate<IZipUploadJobService>(
            "cleanup-orphaned-zip-upload-jobs",
            service => service.CleanupOrphanedJobsAsync(),
            "0 * * * *"); // Every hour
    }

    /// <summary>
    /// Configures attachment indexing recurring jobs.
    /// </summary>
    private static void ConfigureAttachmentIndexingJobs()
    {
        RecurringJob.AddOrUpdate<AttachmentIndexingBackgroundService>(
            "process-unhashed-attachments",
            service => service.ProcessUnhashedAttachmentsAsync(),
            "*/5 * * * *"); // Every 5 minutes
    }

    /// <summary>
    /// Configures attachment preview generation recurring jobs.
    /// </summary>
    private static void ConfigureAttachmentPreviewJobs()
    {
        RecurringJob.AddOrUpdate<AttachmentPreviewBackgroundService>(
            "generate-missing-attachment-previews",
            service => service.ProcessMissingPreviewsAsync(),
            "*/5 * * * *"); // Every 5 minutes
    }

    /// <summary>
    /// Configures all Hangfire-related middleware and jobs.
    /// </summary>
    /// <returns></returns>
    public static IApplicationBuilder UseHangfire(this IApplicationBuilder app, IConfiguration configuration)
    {
        // Configure Hangfire dashboard
        app.UseHangfireDashboardWithAuth(configuration);

        // Configure recurring jobs
        app.ConfigureHangfireRecurringJobs(configuration);

        return app;
    }
}
