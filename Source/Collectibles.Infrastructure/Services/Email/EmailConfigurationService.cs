using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration.Email;

using Microsoft.Extensions.Options;

namespace Collectibles.Infrastructure.Services.Email;

public class EmailConfigurationService : IEmailConfigurationService
{
    private readonly EmailSettings _emailSettings;

    public EmailConfigurationService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    public bool IsEmailConfigured()
    {
        if (_emailSettings == null || string.IsNullOrEmpty(_emailSettings.Provider))
        {
            return false;
        }

        return _emailSettings.Provider.ToLowerInvariant() switch
        {
            "smtp" => !string.IsNullOrEmpty(_emailSettings.Smtp?.Host) &&
                      !string.IsNullOrEmpty(_emailSettings.Smtp?.Username),
            "sendgrid" => !string.IsNullOrEmpty(_emailSettings.SendGrid?.ApiKey),
            "azurecommunication" => !string.IsNullOrEmpty(_emailSettings.AzureCommunication?.ConnectionString),
            _ => false,
        };
    }

    public string GetEmailNotConfiguredMessage()
    {
        return "Email functionality is currently disabled.";
    }

    public string GetEmailNotConfiguredDetailMessage()
    {
        return "Email services are not configured for this application. Please contact your administrator if you need assistance with account confirmation or password reset.";
    }
}
