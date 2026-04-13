using System.Text.Json;

using Collectibles.Application.Common.Models.Email;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration.Email;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Collectibles.Infrastructure.Services.Email;

public class EmailService : IEmailService
{
    private readonly EmailServiceFactory _emailServiceFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly IEmailTemplateService _templateService;

    public EmailService(
        EmailServiceFactory emailServiceFactory,
        IServiceProvider serviceProvider,
        IOptions<EmailSettings> emailSettings,
        ILogger<EmailService> logger,
        IEmailTemplateService templateService)
    {
        _emailServiceFactory = emailServiceFactory;
        _serviceProvider = serviceProvider;
        _emailSettings = emailSettings.Value;
        _logger = logger;
        _templateService = templateService;
    }

    public async Task<EmailResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (_emailSettings.EnableEmailLogging)
        {
            return await SendEmailWithLoggingAsync(message, null, cancellationToken);
        }

        var emailService = _emailServiceFactory.CreateEmailService();
        return await emailService.SendEmailAsync(message, cancellationToken);
    }

    public async Task<EmailResult> SendTemplatedEmailAsync(TemplatedEmailMessage message, CancellationToken cancellationToken = default)
    {
        if (_emailSettings.EnableEmailLogging)
        {
            return await SendEmailWithLoggingAsync(message, message.TemplateName, cancellationToken);
        }

        var emailService = _emailServiceFactory.CreateEmailService();
        return await emailService.SendTemplatedEmailAsync(message, cancellationToken);
    }

    public async Task<List<EmailResult>> SendBulkEmailAsync(List<EmailMessage> messages, CancellationToken cancellationToken = default)
    {
        var emailService = _emailServiceFactory.CreateEmailService();
        var results = new List<EmailResult>();

        foreach (var message in messages)
        {
            var result = await SendEmailAsync(message, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    private async Task<EmailResult> SendEmailWithLoggingAsync(
        EmailMessage message,
        string? templateName,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Render template if this is a templated email
        string? renderedBody = message.Body;
        string? renderedSubject = message.Subject;
        TemplatedEmailMessage? templatedMessage = message as TemplatedEmailMessage;

        if (templatedMessage != null)
        {
            renderedBody = await _templateService.RenderTemplateAsync(
                templatedMessage.TemplateName,
                templatedMessage.TemplateModel,
                cancellationToken);

            if (string.IsNullOrEmpty(renderedSubject))
            {
                renderedSubject = await _templateService.GetTemplateSubjectAsync(
                    templatedMessage.TemplateName,
                    templatedMessage.TemplateModel,
                    cancellationToken);
            }
        }

        var emailLog = new EmailLog
        {
            ToEmail = message.ToEmail,
            ToName = message.ToName,
            CcEmails = message.CcEmails.Count != 0 ? string.Join(";", message.CcEmails) : null,
            BccEmails = message.BccEmails.Count != 0 ? string.Join(";", message.BccEmails) : null,
            FromEmail = message.FromEmail ?? _emailSettings.Sender.DefaultFromEmail,
            FromName = message.FromName ?? _emailSettings.Sender.DefaultFromName,
            Subject = renderedSubject,
            Body = renderedBody,
            IsHtml = message.IsHtml,
            Provider = _emailSettings.Provider,
            Status = EmailStatus.Pending,
            Priority = message.Priority,
            ScheduledFor = message.ScheduledFor,
            TemplateName = templateName,
        };

        if (templatedMessage != null)
        {
            emailLog.TemplateData = JsonSerializer.Serialize(templatedMessage.TemplateModel);
        }

        dbContext.EmailLogs.Add(emailLog);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            emailLog.Status = EmailStatus.InProgress;
            emailLog.AttemptCount++;
            emailLog.LastAttemptAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            var emailService = _emailServiceFactory.CreateEmailService();
            var result = message is TemplatedEmailMessage templated
                ? await emailService.SendTemplatedEmailAsync(templated, cancellationToken)
                : await emailService.SendEmailAsync(message, cancellationToken);

            if (result.IsSuccess)
            {
                emailLog.Status = EmailStatus.Sent;
                emailLog.SentAt = result.SentAt;
                emailLog.MessageId = result.MessageId;
            }
            else
            {
                emailLog.Status = EmailStatus.Failed;
                emailLog.ErrorMessage = result.ErrorMessage;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", message.ToEmail);

            emailLog.Status = EmailStatus.Failed;
            emailLog.ErrorMessage = ex.Message;
            await dbContext.SaveChangesAsync(cancellationToken);

            return EmailResult.Failure(ex.Message, _emailSettings.Provider);
        }
    }
}
