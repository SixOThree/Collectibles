using Collectibles.Application.Interfaces;
using Collectibles.Infrastructure.Services;
using Collectibles.Infrastructure.Services.Email;
using Collectibles.Web.Authorization;

using Hangfire;
using Hangfire.Common;
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
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    public static async Task<IApplicationBuilder> ConfigureHangfireRecurringJobsAsync(this IApplicationBuilder app, IConfiguration configuration)
    {
        var logger = app.ApplicationServices.GetRequiredService<ILogger<Program>>();
        var schemaInitializer = app.ApplicationServices.GetRequiredService<IHangfireSchemaInitializer>();
        var recurringJobManager = app.ApplicationServices.GetRequiredService<IRecurringJobManager>();
        logger.LogInformation("Configuring Hangfire recurring jobs...");

        try
        {
            await schemaInitializer.EnsureSchemaAsync();

            // Email processing jobs
            ConfigureEmailJobs(recurringJobManager);

            // Cleanup jobs
            ConfigureCleanupJobs(recurringJobManager, configuration);

            // Zip upload jobs
            ConfigureZipUploadJobs(recurringJobManager);

            // Attachment indexing jobs
            ConfigureAttachmentIndexingJobs(recurringJobManager);

            // Attachment preview generation jobs
            ConfigureAttachmentPreviewJobs(recurringJobManager);

            // Reclaim soft-deleted attachments past their retention window
            ConfigureAttachmentPurgeJobs(recurringJobManager);

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
    private static void ConfigureEmailJobs(IRecurringJobManager recurringJobManager)
    {
        AddOrUpdate(
            recurringJobManager,
            "process-pending-emails",
            Job.FromExpression<EmailBackgroundService>(service => service.ProcessPendingEmailsAsync()),
            "*/1 * * * *"); // Every minute

        AddOrUpdate(
            recurringJobManager,
            "cleanup-old-email-logs",
            Job.FromExpression<EmailBackgroundService>(service => service.CleanupOldEmailLogsAsync()),
            "0 2 * * *"); // Daily at 2 AM
    }

    /// <summary>
    /// Configures cleanup-related recurring jobs.
    /// </summary>
    private static void ConfigureCleanupJobs(IRecurringJobManager recurringJobManager, IConfiguration configuration)
    {
        var requestLogRetentionDays = configuration.GetValue<int>("Logging:RequestLogRetentionDays", 365);
        if (requestLogRetentionDays <= 0)
        {
            requestLogRetentionDays = 365;
        }

        AddOrUpdate(
            recurringJobManager,
            "cleanup-old-request-logs",
            Job.FromExpression<IRequestLogService>(service => service.CleanupOldLogsAsync(requestLogRetentionDays, CancellationToken.None)),
            "0 3 * * *"); // Daily at 3 AM
    }

    /// <summary>
    /// Configures zip upload-related recurring jobs.
    /// </summary>
    private static void ConfigureZipUploadJobs(IRecurringJobManager recurringJobManager)
    {
        AddOrUpdate(
            recurringJobManager,
            "cleanup-orphaned-zip-upload-jobs",
            Job.FromExpression<IZipUploadJobService>(service => service.CleanupOrphanedJobsAsync()),
            "0 * * * *"); // Every hour
    }

    /// <summary>
    /// Configures attachment indexing recurring jobs.
    /// </summary>
    private static void ConfigureAttachmentIndexingJobs(IRecurringJobManager recurringJobManager)
    {
        AddOrUpdate(
            recurringJobManager,
            "process-unhashed-attachments",
            Job.FromExpression<AttachmentIndexingBackgroundService>(service => service.ProcessUnhashedAttachmentsAsync()),
            "*/5 * * * *"); // Every 5 minutes
    }

    /// <summary>
    /// Configures attachment preview generation recurring jobs.
    /// </summary>
    private static void ConfigureAttachmentPreviewJobs(IRecurringJobManager recurringJobManager)
    {
        AddOrUpdate(
            recurringJobManager,
            "generate-missing-attachment-previews",
            Job.FromExpression<AttachmentPreviewBackgroundService>(service => service.ProcessMissingPreviewsAsync()),
            "*/5 * * * *"); // Every 5 minutes
    }

    /// <summary>
    /// Configures the purge of soft-deleted attachments past their retention window.
    /// </summary>
    private static void ConfigureAttachmentPurgeJobs(IRecurringJobManager recurringJobManager)
    {
        AddOrUpdate(
            recurringJobManager,
            "purge-deleted-attachments",
            Job.FromExpression<AttachmentPurgeBackgroundService>(service => service.PurgeDeletedAttachmentsAsync()),
            "30 3 * * *"); // Daily at 3:30 AM
    }

    private static void AddOrUpdate(IRecurringJobManager recurringJobManager, string recurringJobId, Job job, string cronExpression)
    {
        recurringJobManager.AddOrUpdate(recurringJobId, job, cronExpression, new RecurringJobOptions());
    }

    /// <summary>
    /// Configures all Hangfire-related middleware and jobs.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    public static async Task<IApplicationBuilder> UseHangfireAsync(this IApplicationBuilder app, IConfiguration configuration)
    {
        // Configure Hangfire dashboard
        app.UseHangfireDashboardWithAuth(configuration);

        // Configure recurring jobs
        await app.ConfigureHangfireRecurringJobsAsync(configuration);

        return app;
    }
}
