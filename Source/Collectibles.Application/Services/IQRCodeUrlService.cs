namespace Collectibles.Application.Services;

public interface IQRCodeUrlService
{
    string GenerateQRCodeUrl(string code);
}
