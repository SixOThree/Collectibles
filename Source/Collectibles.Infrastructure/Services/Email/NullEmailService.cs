using System.Text.Json;

using Collectibles.Application.Common.Models.Email;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration.Email;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Collectibles.Infrastructure.Services.Email;

public class NullEmailService : IEmailService
{
    private readonly ILogger<NullEmailService> _logger;
    private readonly EmailSenderSettings _senderSettings;

    public NullEmailService(
        IOptions<EmailSettings> emailSettings,
        ILogger<NullEmailService> logger)
    {
        _senderSettings = emailSettings.Value.Sender;
        _logger = logger;
    }

    public Task<EmailResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var fromEmail = message.FromEmail ?? _senderSettings.DefaultFromEmail;
        var fromName = message.FromName ?? _senderSettings.DefaultFromName;

        _logger.LogInformation(
            "NULL EMAIL SERVICE - Email NOT sent (logged only): From: {FromName} <{FromEmail}>, To: {ToName} <{ToEmail}>, Subject: {Subject}",
            fromName, fromEmail, message.ToName ?? message.ToEmail, message.ToEmail, message.Subject);

        if (message.CcEmails.Count != 0)
        {
            _logger.LogInformation("  CC: {CcEmails}", string.Join(", ", message.CcEmails));
        }

        if (message.BccEmails.Count != 0)
        {
            _logger.LogInformation("  BCC: {BccEmails}", string.Join(", ", message.BccEmails));
        }

        _logger.LogDebug(
            "  Body ({BodyType}): {Body}",
            message.IsHtml ? "HTML" : "Text",
            message.Body.Length > 500 ? string.Concat(message.Body.AsSpan(0, 500), "...") : message.Body);

        if (message.Attachments.Count != 0)
        {
            _logger.LogInformation(
                "  Attachments: {Attachments}",
                string.Join(", ", message.Attachments.Select(a => $"{a.FileName} ({a.ContentType})")));
        }

        var mockMessageId = $"NULL-{Guid.NewGuid():N}";

        return Task.FromResult(EmailResult.Success(mockMessageId, "NULL"));
    }

    public async Task<EmailResult> SendTemplatedEmailAsync(
        TemplatedEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var processedBody = ProcessTemplate(message.Body, message.TemplateModel);
        var processedSubject = ProcessTemplate(message.Subject, message.TemplateModel);

        _logger.LogInformation("NULL EMAIL SERVICE - Processing template: {TemplateName}", message.TemplateName);
        _logger.LogDebug(
            "  Template Model: {TemplateModel}",
            JsonSerializer.Serialize(message.TemplateModel));

        var emailMessage = new EmailMessage
        {
            ToEmail = message.ToEmail,
            ToName = message.ToName,
            CcEmails = message.CcEmails,
            BccEmails = message.BccEmails,
            FromEmail = message.FromEmail,
            FromName = message.FromName,
            Subject = processedSubject,
            Body = processedBody,
            IsHtml = message.IsHtml,
            Attachments = message.Attachments,
            Headers = message.Headers,
            Priority = message.Priority,
            ScheduledFor = message.ScheduledFor,
        };

        return await SendEmailAsync(emailMessage, cancellationToken);
    }

    public async Task<List<EmailResult>> SendBulkEmailAsync(
        List<EmailMessage> messages,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "NULL EMAIL SERVICE - Processing bulk email batch of {Count} messages",
            messages.Count);

        var results = new List<EmailResult>();

        foreach (var message in messages)
        {
            var result = await SendEmailAsync(message, cancellationToken);
            results.Add(result);

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }

        return results;
    }

    private static string ProcessTemplate(string template, object model)
    {
        if (model == null || string.IsNullOrEmpty(template))
        {
            return template ?? string.Empty;
        }

        var result = template;
        var properties = model.GetType().GetProperties();

        foreach (var property in properties)
        {
            var placeholder = $"{{{{{property.Name}}}}}";
            var value = property.GetValue(model)?.ToString() ?? string.Empty;
            result = result.Replace(placeholder, value);
        }

        return result;
    }
}
