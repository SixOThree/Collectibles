namespace Collectibles.Application.Interfaces;

public interface IEmailConfigurationService
{
    bool IsEmailConfigured();
    string GetEmailNotConfiguredMessage();
    string GetEmailNotConfiguredDetailMessage();
}
