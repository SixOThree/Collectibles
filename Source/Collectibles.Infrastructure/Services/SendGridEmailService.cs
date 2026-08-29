using Collectibles.Application.Common.Models.Email;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration.Email;
using Collectibles.Domain.Constants;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SendGrid;
using SendGrid.Helpers.Mail;

namespace Collectibles.Infrastructure.Services;

public class SendGridEmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<SendGridEmailService> _logger;
    private readonly IEmailTemplateService _templateService;
    private readonly ISendGridClient _sendGridClient;

    public SendGridEmailService(
        IOptions<EmailSettings> emailSettings,
        ILogger<SendGridEmailService> logger,
        IEmailTemplateService templateService,
        ISendGridClient sendGridClient)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
        _templateService = templateService;

        // Injected rather than constructed here. `new SendGridClient(apiKey)` allocates its
        // own HttpClient, and this service is scoped, so under load every request scope
        // created a fresh client - accumulating TIME_WAIT sockets and pinning stale DNS.
        _sendGridClient = sendGridClient;
    }

    public async Task<EmailResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var sendGridMessage = CreateSendGridMessage(message);

            if (_emailSettings.SendGrid.SandboxMode)
            {
                sendGridMessage.SetSandBoxMode(true);
            }

            var response = await _sendGridClient.SendEmailAsync(sendGridMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Email sent successfully via SendGrid to {ToEmail} with subject: {Subject}",
                    message.ToEmail, message.Subject);

                string? messageId = null;
                if (response.Headers.TryGetValues("X-Message-Id", out var messageIds))
                {
                    messageId = messageIds.FirstOrDefault();
                }

                return EmailResult.Success(messageId, "SendGrid");
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "SendGrid API error: {StatusCode} - {Response}",
                    response.StatusCode, responseBody);

                return EmailResult.Failure($"SendGrid API error: {response.StatusCode}", "SendGrid");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via SendGrid to {ToEmail}", message.ToEmail);
            return EmailResult.Failure(ex.Message, "SendGrid");
        }
    }

    public async Task<EmailResult> SendTemplatedEmailAsync(TemplatedEmailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            message.Body = await _templateService.RenderTemplateAsync(
                message.TemplateName,
                message.TemplateModel,
                cancellationToken);

            if (string.IsNullOrEmpty(message.Subject))
            {
                message.Subject = await _templateService.GetTemplateSubjectAsync(
                    message.TemplateName,
                    message.TemplateModel,
                    cancellationToken);
            }

            return await SendEmailAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send templated email via SendGrid to {ToEmail}", message.ToEmail);
            return EmailResult.Failure(ex.Message, "SendGrid");
        }
    }

    public async Task<List<EmailResult>> SendBulkEmailAsync(List<EmailMessage> messages, CancellationToken cancellationToken = default)
    {
        var results = new List<EmailResult>();

        var batches = messages.Chunk(ApplicationConstants.BatchProcessing.SendGridBatchSize);

        foreach (var batch in batches)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            foreach (var message in batch)
            {
                var result = await SendEmailAsync(message, cancellationToken);
                results.Add(result);
            }
        }

        return results;
    }

    private SendGridMessage CreateSendGridMessage(EmailMessage message)
    {
        var sendGridMessage = new SendGridMessage();

        var fromEmail = message.FromEmail ?? _emailSettings.Sender.DefaultFromEmail;
        var fromName = message.FromName ?? _emailSettings.Sender.DefaultFromName;
        sendGridMessage.SetFrom(new EmailAddress(fromEmail, fromName));

        sendGridMessage.AddTo(new EmailAddress(message.ToEmail, message.ToName));

        foreach (var cc in message.CcEmails)
        {
            sendGridMessage.AddCc(new EmailAddress(cc));
        }

        foreach (var bcc in message.BccEmails)
        {
            sendGridMessage.AddBcc(new EmailAddress(bcc));
        }

        if (!string.IsNullOrEmpty(_emailSettings.Sender.ReplyToEmail))
        {
            sendGridMessage.SetReplyTo(new EmailAddress(
                _emailSettings.Sender.ReplyToEmail,
                _emailSettings.Sender.ReplyToName));
        }

        sendGridMessage.SetSubject(message.Subject);

        if (message.IsHtml)
        {
            sendGridMessage.AddContent(MimeType.Html, message.Body);
        }
        else
        {
            sendGridMessage.AddContent(MimeType.Text, message.Body);
        }

        foreach (var header in message.Headers)
        {
            sendGridMessage.AddHeader(header.Key, header.Value);
        }

        foreach (var attachment in message.Attachments)
        {
            sendGridMessage.AddAttachment(new SendGrid.Helpers.Mail.Attachment
            {
                Content = Convert.ToBase64String(attachment.Content),
                Type = attachment.ContentType,
                Filename = attachment.FileName,
                Disposition = "attachment",
            });
        }

        if (message.Priority > 0)
        {
            sendGridMessage.AddHeader("X-Priority", message.Priority.ToString());
        }

        return sendGridMessage;
    }
}
