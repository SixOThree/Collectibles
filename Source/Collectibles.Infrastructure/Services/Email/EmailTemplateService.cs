using System.Reflection;

using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Collectibles.Infrastructure.Services.Email;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly ILogger<EmailTemplateService> _logger;
    private readonly SiteSettings _siteSettings;

    public EmailTemplateService(ILogger<EmailTemplateService> logger, IOptions<SiteSettings> siteSettings)
    {
        _logger = logger;
        _siteSettings = siteSettings.Value;
    }

    public Task<string> RenderTemplateAsync(string templateName, object model, CancellationToken cancellationToken = default)
    {
        var template = GetBasicTemplate(templateName, model);
        return Task.FromResult(template);
    }

    public Task<bool> TemplateExistsAsync(string templateName, CancellationToken cancellationToken = default)
    {
        var knownTemplates = new[] { "Welcome", "PasswordReset", "EmailConfirmation", "Notification" };
        return Task.FromResult(knownTemplates.Contains(templateName, StringComparer.OrdinalIgnoreCase));
    }

    public Task<string> GetTemplateSubjectAsync(string templateName, object model, CancellationToken cancellationToken = default)
    {
        var subject = templateName.ToLowerInvariant() switch
        {
            "welcome" => $"Welcome to {_siteSettings.SiteTitle}!",
            "passwordreset" => "Reset Your Password",
            "emailconfirmation" => "Confirm Your Email Address",
            "notification" => "Important Notification",
            _ => $"Message from {_siteSettings.SiteTitle}",
        };

        return Task.FromResult(subject);
    }

    private string GetBasicTemplate(string templateName, object model)
    {
        var baseTemplate = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{TITLE}</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #007bff; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background-color: #f8f9fa; }
        .footer { text-align: center; padding: 20px; font-size: 12px; color: #666; }
        .button { display: inline-block; padding: 10px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>{SITENAME}</h1>
        </div>
        <div class=""content"">
            {CONTENT}
        </div>
        <div class=""footer"">
            <p>&copy; {YEAR} {SITENAME}. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        var content = GetTemplateContent(templateName, model);
        var title = GetTemplateTitle(templateName);

        return baseTemplate
            .Replace("{TITLE}", title)
            .Replace("{CONTENT}", content)
            .Replace("{SITENAME}", _siteSettings.SiteName)
            .Replace("{YEAR}", DateTime.UtcNow.Year.ToString());
    }

    private string GetTemplateTitle(string templateName)
    {
        return templateName.ToLowerInvariant() switch
        {
            "welcome" => "Welcome",
            "passwordreset" => "Password Reset",
            "emailconfirmation" => "Email Confirmation",
            "notification" => "Notification",
            _ => _siteSettings.SiteTitle,
        };
    }

    private static string GetTemplateContent(string templateName, object model)
    {
        return templateName.ToLowerInvariant() switch
        {
            "welcome" => GetWelcomeContent(model),
            "passwordreset" => GetPasswordResetContent(model),
            "emailconfirmation" => GetEmailConfirmationContent(model),
            "notification" => GetNotificationContent(model),
            _ => "<p>Thank you for using Collectibles.</p>",
        };
    }

    private static string GetWelcomeContent(object model)
    {
        var name = GetPropertyValue(model, "Name") ?? "User";
        return $@"
            <h2>Welcome, {name}!</h2>
            <p>Thank you for joining Collectibles. We're excited to have you as part of our community.</p>
            <p>Start exploring and managing your collection today!</p>
            <p><a href=""#"" class=""button"">Get Started</a></p>";
    }

    private static string GetPasswordResetContent(object model)
    {
        var resetLink = GetPropertyValue(model, "ResetLink") ?? "#";
        return $@"
            <h2>Reset Your Password</h2>
            <p>We received a request to reset your password. Click the button below to create a new password:</p>
            <p><a href=""{resetLink}"" class=""button"">Reset Password</a></p>
            <p>If you didn't request this, please ignore this email.</p>
            <p>This link will expire in 24 hours.</p>";
    }

    private static string GetEmailConfirmationContent(object model)
    {
        var confirmLink = GetPropertyValue(model, "ConfirmLink") ?? "#";
        return $@"
            <h2>Confirm Your Email Address</h2>
            <p>Please confirm your email address by clicking the button below:</p>
            <p><a href=""{confirmLink}"" class=""button"">Confirm Email</a></p>
            <p>This helps us ensure we can reach you with important updates about your account.</p>";
    }

    private static string GetNotificationContent(object model)
    {
        var message = GetPropertyValue(model, "Message") ?? "You have a new notification.";
        return $@"
            <h2>Notification</h2>
            <p>{message}</p>";
    }

    private static string? GetPropertyValue(object model, string propertyName)
    {
        if (model == null)
        {
            return null;
        }

        var property = model.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(model)?.ToString();
    }
}
