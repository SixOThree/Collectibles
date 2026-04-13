using Azure;
using Azure.Communication.Email;
using Collectibles.Application.Common.Models.Email;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using AppEmailMessage = Collectibles.Application.Common.Models.Email.EmailMessage;
using AzureEmailAttachment = Azure.Communication.Email.EmailAttachment;
using AzureEmailMessage = Azure.Communication.Email.EmailMessage;

namespace Collectibles.Infrastructure.Services.Email;

public class AzureCommunicationEmailService : IEmailService
{
    private readonly EmailClient _emailClient;
    private readonly AzureCommunicationSettings _settings;
    private readonly EmailSenderSettings _senderSettings;
    private readonly ILogger<AzureCommunicationEmailService> _logger;

    public AzureCommunicationEmailService(
        IOptions<EmailSettings> emailSettings,
        ILogger<AzureCommunicationEmailService> logger)
    {
        _settings = emailSettings.Value.AzureCommunication;
        _senderSettings = emailSettings.Value.Sender;
        _logger = logger;

        if (string.IsNullOrEmpty(_settings.ConnectionString))
        {
            throw new InvalidOperationException("Azure Communication Services connection string is not configured.");
        }

        _emailClient = new EmailClient(_settings.ConnectionString);
    }

    public async Task<EmailResult> SendEmailAsync(AppEmailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var emailContent = new EmailContent(message.Subject);

            if (message.IsHtml)
            {
                emailContent.Html = message.Body;
            }
            else
            {
                emailContent.PlainText = message.Body;
            }

            var toRecipients = new List<EmailAddress>
            {
                new EmailAddress(message.ToEmail, message.ToName),
            };

            var ccRecipients = message.CcEmails
                .Select(email => new EmailAddress(email))
                .ToList();

            var bccRecipients = message.BccEmails
                .Select(email => new EmailAddress(email))
                .ToList();

            var emailRecipients = new EmailRecipients(toRecipients);

            if (ccRecipients.Count != 0)
            {
                foreach (var cc in ccRecipients)
                {
                    emailRecipients.CC.Add(cc);
                }
            }

            if (bccRecipients.Count != 0)
            {
                foreach (var bcc in bccRecipients)
                {
                    emailRecipients.BCC.Add(bcc);
                }
            }

            var senderAddress = !string.IsNullOrEmpty(message.FromEmail)
                ? message.FromEmail
                : _settings.SenderAddress;

            if (string.IsNullOrEmpty(senderAddress))
            {
                senderAddress = _senderSettings.DefaultFromEmail;
            }

            var emailMessage = new AzureEmailMessage(
                senderAddress,
                emailRecipients,
                emailContent);

            // Set sender display name using custom headers
            if (!string.IsNullOrEmpty(message.FromName))
            {
                emailMessage.Headers.Add("From", $"{message.FromName} <{senderAddress}>");
            }
            else if (!string.IsNullOrEmpty(_senderSettings.DefaultFromName))
            {
                emailMessage.Headers.Add("From", $"{_senderSettings.DefaultFromName} <{senderAddress}>");
            }

            if (!string.IsNullOrEmpty(_senderSettings.ReplyToEmail))
            {
                emailMessage.ReplyTo.Add(new EmailAddress(
                    _senderSettings.ReplyToEmail,
                    _senderSettings.ReplyToName));
            }

            foreach (var attachment in message.Attachments)
            {
                var emailAttachment = new AzureEmailAttachment(
                    attachment.FileName,
                    attachment.ContentType,
                    new BinaryData(attachment.Content));

                emailMessage.Attachments.Add(emailAttachment);
            }

            foreach (var header in message.Headers)
            {
                if (header.Key != "From") // Don't duplicate the From header
                {
                    emailMessage.Headers.Add(header.Key, header.Value);
                }
            }

            var operation = await _emailClient.SendAsync(
                WaitUntil.Started,
                emailMessage,
                cancellationToken);

            var pollingInterval = TimeSpan.FromSeconds(_settings.PollingIntervalSeconds);
            var timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
            var endTime = DateTimeOffset.UtcNow.Add(timeout);

            while (!operation.HasCompleted && DateTimeOffset.UtcNow < endTime)
            {
                await Task.Delay(pollingInterval, cancellationToken);
                await operation.UpdateStatusAsync(cancellationToken);
            }

            if (operation.HasValue && operation.Value.Status == EmailSendStatus.Succeeded)
            {
                _logger.LogInformation(
                    "Email sent successfully to {ToEmail} via Azure Communication Services",
                    message.ToEmail);

                // The operation ID can be used as the message ID
                var messageId = operation.Id;

                return EmailResult.Success(
                    messageId,
                    "AzureCommunication");
            }

            if (operation.HasValue && operation.Value.Status == EmailSendStatus.Failed)
            {
                var error = "Email send failed";
                _logger.LogError(
                    "Failed to send email to {ToEmail}: {Error}",
                    message.ToEmail, error);

                return EmailResult.Failure(error, "AzureCommunication");
            }

            _logger.LogWarning("Email send operation timed out for {ToEmail}", message.ToEmail);
            return EmailResult.Failure("Email send operation timed out", "AzureCommunication");
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Communication Services request failed for email to {ToEmail}",
                message.ToEmail);
            return EmailResult.Failure(ex.Message, "AzureCommunication");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {ToEmail}", message.ToEmail);
            return EmailResult.Failure(ex.Message, "AzureCommunication");
        }
    }

    public async Task<EmailResult> SendTemplatedEmailAsync(
        TemplatedEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var processedBody = ProcessTemplate(message.Body, message.TemplateModel);
        var processedSubject = ProcessTemplate(message.Subject, message.TemplateModel);

        var emailMessage = new AppEmailMessage
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
        List<AppEmailMessage> messages,
        CancellationToken cancellationToken = default)
    {
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
            return template;
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
