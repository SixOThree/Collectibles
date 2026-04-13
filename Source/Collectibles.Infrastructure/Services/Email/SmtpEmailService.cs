using System.Net.Security;

using Collectibles.Application.Common.Models.Email;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration.Email;

using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MimeKit;

namespace Collectibles.Infrastructure.Services.Email;

public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly IEmailTemplateService _templateService;

    public SmtpEmailService(
        IOptions<EmailSettings> emailSettings,
        ILogger<SmtpEmailService> logger,
        IEmailTemplateService templateService)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
        _templateService = templateService;
    }

    public async Task<EmailResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var mimeMessage = await CreateMimeMessage(message, cancellationToken);

            using var client = new SmtpClient();

            client.ServerCertificateValidationCallback = (sender, certificate, chain, errors) =>
            {
                if (errors == SslPolicyErrors.None)
                {
                    return true;
                }

                _logger.LogWarning("SMTP certificate validation error: {Errors}", errors);
                return false;
            };

            await client.ConnectAsync(
                _emailSettings.Smtp.Host,
                _emailSettings.Smtp.Port,
                _emailSettings.Smtp.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                cancellationToken);

            if (!string.IsNullOrEmpty(_emailSettings.Smtp.Username))
            {
                await client.AuthenticateAsync(
                    _emailSettings.Smtp.Username,
                    _emailSettings.Smtp.Password,
                    cancellationToken);
            }

            var response = await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation(
                "Email sent successfully to {ToEmail} with subject: {Subject}",
                message.ToEmail, message.Subject);

            return EmailResult.Success(response, "SMTP");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", message.ToEmail);
            return EmailResult.Failure(ex.Message, "SMTP");
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
            _logger.LogError(ex, "Failed to send templated email to {ToEmail}", message.ToEmail);
            return EmailResult.Failure(ex.Message, "SMTP");
        }
    }

    public async Task<List<EmailResult>> SendBulkEmailAsync(List<EmailMessage> messages, CancellationToken cancellationToken = default)
    {
        var results = new List<EmailResult>();

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var result = await SendEmailAsync(message, cancellationToken);
            results.Add(result);

            await Task.Delay(100, cancellationToken);
        }

        return results;
    }

    private Task<MimeMessage> CreateMimeMessage(EmailMessage message, CancellationToken cancellationToken)
    {
        var mimeMessage = new MimeMessage();

        var fromEmail = message.FromEmail ?? _emailSettings.Sender.DefaultFromEmail;
        var fromName = message.FromName ?? _emailSettings.Sender.DefaultFromName;
        mimeMessage.From.Add(new MailboxAddress(fromName, fromEmail));

        mimeMessage.To.Add(new MailboxAddress(message.ToName ?? message.ToEmail, message.ToEmail));

        foreach (var cc in message.CcEmails)
        {
            mimeMessage.Cc.Add(MailboxAddress.Parse(cc));
        }

        foreach (var bcc in message.BccEmails)
        {
            mimeMessage.Bcc.Add(MailboxAddress.Parse(bcc));
        }

        if (!string.IsNullOrEmpty(_emailSettings.Sender.ReplyToEmail))
        {
            mimeMessage.ReplyTo.Add(new MailboxAddress(
                _emailSettings.Sender.ReplyToName ?? _emailSettings.Sender.ReplyToEmail,
                _emailSettings.Sender.ReplyToEmail));
        }

        mimeMessage.Subject = message.Subject;

        foreach (var header in message.Headers)
        {
            mimeMessage.Headers.Add(header.Key, header.Value);
        }

        var bodyBuilder = new BodyBuilder();

        if (message.IsHtml)
        {
            bodyBuilder.HtmlBody = message.Body;
        }
        else
        {
            bodyBuilder.TextBody = message.Body;
        }

        foreach (var attachment in message.Attachments)
        {
            bodyBuilder.Attachments.Add(
                attachment.FileName,
                attachment.Content,
                ContentType.Parse(attachment.ContentType));
        }

        mimeMessage.Body = bodyBuilder.ToMessageBody();

        if (message.Priority > 0)
        {
            mimeMessage.Priority = message.Priority switch
            {
                1 => MessagePriority.Urgent,
                2 => MessagePriority.Normal,
                _ => MessagePriority.NonUrgent,
            };
        }

        return Task.FromResult(mimeMessage);
    }
}
