using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration.Email;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Collectibles.Infrastructure.Services.Email;

public class EmailServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EmailSettings _emailSettings;

    public EmailServiceFactory(IServiceProvider serviceProvider, IOptions<EmailSettings> emailSettings)
    {
        _serviceProvider = serviceProvider;
        _emailSettings = emailSettings.Value;
    }

    public IEmailService CreateEmailService()
    {
        // Provider names are configuration identifiers, not user-facing text: case-folding
        // them with the current culture made "AzureCommunication" fail to match under
        // tr-TR (dotless i) and silently fall through to SMTP.
        return _emailSettings.Provider.ToUpperInvariant() switch
        {
            "SENDGRID" => _serviceProvider.GetRequiredService<SendGridEmailService>(),
            "SMTP" => _serviceProvider.GetRequiredService<SmtpEmailService>(),
            "AZURECOMMUNICATION" => _serviceProvider.GetRequiredService<AzureCommunicationEmailService>(),
            "NULL" => _serviceProvider.GetRequiredService<NullEmailService>(),
            _ => _serviceProvider.GetRequiredService<SmtpEmailService>(),
        };
    }
}
