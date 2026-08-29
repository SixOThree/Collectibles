using System.Text.Json;

using Collectibles.Application.Common.Models.Email;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration.Email;

using Hangfire;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Collectibles.Infrastructure.Services.Email;

public class EmailBackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailBackgroundService> _logger;
    private readonly EmailSettings _emailSettings;

    public EmailBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<EmailBackgroundService> logger,
        IOptions<EmailSettings> emailSettings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _emailSettings = emailSettings.Value;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessPendingEmailsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var sysLogService = scope.ServiceProvider.GetRequiredService<ISysLogService>();

        var pendingEmails = await dbContext.EmailLogs
            .Where(e => e.Status == EmailStatus.Pending ||
                       (e.Status == EmailStatus.Failed && e.AttemptCount < _emailSettings.Retry.MaxAttempts))
            .Where(e => e.ScheduledFor == null || e.ScheduledFor <= DateTime.UtcNow)
            .OrderBy(e => e.Priority)
            .ThenBy(e => e.Created)
            .Take(50)
            .ToListAsync();

        if (pendingEmails.Count != 0)
        {
            await sysLogService.LogDebugAsync(
                $"Processing {pendingEmails.Count} pending emails",
                "Email.Processing",
                new Dictionary<string, object> { ["Count"] = pendingEmails.Count });
        }

        foreach (var emailLog in pendingEmails)
        {
            BackgroundJob.Enqueue(() => SendEmailWithRetryAsync(emailLog.Id));
        }
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task SendEmailWithRetryAsync(long emailLogId)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var emailServiceFactory = scope.ServiceProvider.GetRequiredService<EmailServiceFactory>();
        var sysLogService = scope.ServiceProvider.GetRequiredService<ISysLogService>();

        var emailLog = await dbContext.EmailLogs.FindAsync(emailLogId);
        if (emailLog == null)
        {
            _logger.LogWarning("EmailLog with ID {EmailLogId} not found", emailLogId);
            await sysLogService.LogWarningAsync(
                $"EmailLog with ID {emailLogId} not found",
                "Email.Processing");
            return;
        }

        // Claim the row atomically before sending. The previous read-then-write guard let
        // the recurring scan, an inline send, and a scheduled retry all pick up the same
        // row and deliver it more than once - including password resets.
        var claimed = await TryClaimAsync(dbContext, emailLogId);
        if (!claimed)
        {
            _logger.LogInformation("EmailLog {EmailLogId} is already claimed by another worker", emailLogId);
            return;
        }

        await ((DbContext)dbContext).Entry(emailLog).ReloadAsync(CancellationToken.None);

        try
        {
            var emailMessage = CreateEmailMessage(emailLog);
            var emailService = emailServiceFactory.CreateEmailService();

            EmailResult result;

            // If body is already rendered (stored from original send), use it directly
            if (!string.IsNullOrEmpty(emailLog.Body))
            {
                result = await emailService.SendEmailAsync(emailMessage);
            }

            // Otherwise, render template (backwards compatibility for old EmailLog records)
            else if (!string.IsNullOrEmpty(emailLog.TemplateName) && !string.IsNullOrEmpty(emailLog.TemplateData))
            {
                var templatedMessage = new TemplatedEmailMessage
                {
                    ToEmail = emailMessage.ToEmail,
                    ToName = emailMessage.ToName,
                    CcEmails = emailMessage.CcEmails,
                    BccEmails = emailMessage.BccEmails,
                    FromEmail = emailMessage.FromEmail,
                    FromName = emailMessage.FromName,
                    Subject = emailMessage.Subject,
                    Body = emailMessage.Body,
                    IsHtml = emailMessage.IsHtml,
                    TemplateName = emailLog.TemplateName,
                    TemplateModel = JsonSerializer.Deserialize<object>(emailLog.TemplateData) ?? new { },
                };
                result = await emailService.SendTemplatedEmailAsync(templatedMessage);
            }
            else
            {
                result = await emailService.SendEmailAsync(emailMessage);
            }

            if (result.IsSuccess)
            {
                emailLog.Status = EmailStatus.Sent;
                emailLog.SentAt = result.SentAt;
                emailLog.MessageId = result.MessageId;
                emailLog.ErrorMessage = null;
                await dbContext.SaveChangesAsync(CancellationToken.None);

                _logger.LogInformation(
                    "Email sent successfully to {ToEmail} (ID: {EmailLogId})",
                    emailLog.ToEmail, emailLogId);

                await sysLogService.LogInformationAsync(
                    $"Email sent successfully to {emailLog.ToEmail}",
                    "Email.Processing",
                    new Dictionary<string, object>
                    {
                        ["EmailLogId"] = emailLogId,
                        ["ToEmail"] = emailLog.ToEmail,
                        ["Subject"] = emailLog.Subject,
                        ["MessageId"] = result.MessageId ?? "N/A",
                    });
            }
            else
            {
                await HandleEmailFailure(emailLog, result.ErrorMessage ?? "Unknown error", dbContext, sysLogService);
            }
        }
        catch (Exception ex)
        {
            await HandleEmailFailure(emailLog, ex.Message, dbContext, sysLogService);
        }
    }

    private async Task HandleEmailFailure(EmailLog emailLog, string errorMessage, IApplicationDbContext dbContext, ISysLogService sysLogService)
    {
        emailLog.ErrorMessage = errorMessage;

        if (emailLog.AttemptCount >= _emailSettings.Retry.MaxAttempts)
        {
            emailLog.Status = EmailStatus.Failed;
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _logger.LogError(
                "Email permanently failed after {Attempts} attempts to {ToEmail} (ID: {EmailLogId}): {Error}",
                emailLog.AttemptCount, emailLog.ToEmail, emailLog.Id, errorMessage);

            await sysLogService.LogErrorAsync(
                $"Email permanently failed after {emailLog.AttemptCount} attempts to {emailLog.ToEmail}",
                null,
                "Email.Processing",
                new Dictionary<string, object>
                {
                    ["EmailLogId"] = emailLog.Id,
                    ["ToEmail"] = emailLog.ToEmail,
                    ["AttemptCount"] = emailLog.AttemptCount,
                    ["Error"] = errorMessage,
                });
        }
        else
        {
            emailLog.Status = EmailStatus.Failed;
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var delay = CalculateRetryDelay(emailLog.AttemptCount);

            _logger.LogWarning(
                "Email failed on attempt {Attempt} to {ToEmail} (ID: {EmailLogId}). Retrying in {Delay} seconds: {Error}",
                emailLog.AttemptCount, emailLog.ToEmail, emailLog.Id, delay, errorMessage);

            await sysLogService.LogWarningAsync(
                $"Email failed on attempt {emailLog.AttemptCount}, scheduling retry",
                "Email.Processing",
                new Dictionary<string, object>
                {
                    ["EmailLogId"] = emailLog.Id,
                    ["ToEmail"] = emailLog.ToEmail,
                    ["AttemptCount"] = emailLog.AttemptCount,
                    ["RetryDelay"] = delay,
                    ["Error"] = errorMessage,
                });

            // Deliberately no BackgroundJob.Schedule here. The recurring scan already
            // re-picks Failed rows that are still under MaxAttempts, and running both gave
            // two overlapping retry mechanisms racing for the same row. ScheduledFor makes
            // the scan honour the backoff.
            emailLog.ScheduledFor = DateTime.UtcNow.AddSeconds(delay);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// Compare-and-swap the row into <see cref="EmailStatus.InProgress"/>. Only the caller
    /// that observes a row actually transitioning may send it.
    /// </summary>
    private static async Task<bool> TryClaimAsync(IApplicationDbContext dbContext, long emailLogId)
    {
        var rowsAffected = await ((DbContext)dbContext).Database.ExecuteSqlRawAsync(
            @"UPDATE EmailLogs
              SET Status = {0}, AttemptCount = AttemptCount + 1, LastAttemptAt = {1}
              WHERE Id = {2} AND Status IN ({3}, {4})",
            (int)EmailStatus.InProgress,
            DateTime.UtcNow,
            emailLogId,
            (int)EmailStatus.Pending,
            (int)EmailStatus.Failed);

        return rowsAffected > 0;
    }

    private int CalculateRetryDelay(int attemptCount)
    {
        var delay = _emailSettings.Retry.InitialDelaySeconds * Math.Pow(_emailSettings.Retry.BackoffMultiplier, attemptCount - 1);
        return (int)Math.Min(delay, _emailSettings.Retry.MaxDelaySeconds);
    }

    private static EmailMessage CreateEmailMessage(EmailLog emailLog)
    {
        var message = new EmailMessage
        {
            ToEmail = emailLog.ToEmail,
            ToName = emailLog.ToName,
            FromEmail = emailLog.FromEmail,
            FromName = emailLog.FromName,
            Subject = emailLog.Subject,
            Body = emailLog.Body ?? string.Empty,
            IsHtml = emailLog.IsHtml,
            Priority = emailLog.Priority,
        };

        if (!string.IsNullOrEmpty(emailLog.CcEmails))
        {
            message.CcEmails.AddRange(emailLog.CcEmails.Split(';', StringSplitOptions.RemoveEmptyEntries));
        }

        if (!string.IsNullOrEmpty(emailLog.BccEmails))
        {
            message.BccEmails.AddRange(emailLog.BccEmails.Split(';', StringSplitOptions.RemoveEmptyEntries));
        }

        return message;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task CleanupOldEmailLogsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var sysLogService = scope.ServiceProvider.GetRequiredService<ISysLogService>();

        var retentionDays = _emailSettings.LogRetentionDays > 0 ? _emailSettings.LogRetentionDays : 365;
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        var oldEmailLogs = await dbContext.EmailLogs
            .Where(e => e.Status == EmailStatus.Sent && e.SentAt < cutoffDate)
            .Take(1000)
            .ToListAsync();

        if (oldEmailLogs.Count != 0)
        {
            dbContext.EmailLogs.RemoveRange(oldEmailLogs);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _logger.LogInformation("Cleaned up {Count} old email logs", oldEmailLogs.Count);

            await sysLogService.LogInformationAsync(
                $"Cleaned up {oldEmailLogs.Count} old email logs",
                "Email.Maintenance",
                new Dictionary<string, object>
                {
                    ["Count"] = oldEmailLogs.Count,
                    ["CutoffDate"] = cutoffDate,
                });
        }
    }
}
