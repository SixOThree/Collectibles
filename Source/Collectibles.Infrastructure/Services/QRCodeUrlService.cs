using Collectibles.Application.Configuration;
using Collectibles.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Collectibles.Infrastructure.Services;

public class QRCodeUrlService : IQRCodeUrlService
{
    private readonly QRCodeSettings _settings;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public QRCodeUrlService(IOptions<QRCodeSettings> settings, IHttpContextAccessor httpContextAccessor)
    {
        _settings = settings.Value;
        _httpContextAccessor = httpContextAccessor;
    }

    public string GenerateQRCodeUrl(string code)
    {
        string baseUrl;

        if (!string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            // Use configured base URL
            baseUrl = _settings.BaseUrl.TrimEnd('/');
        }
        else
        {
            // Fall back to current request URL
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request != null)
            {
                baseUrl = $"{request.Scheme}://{request.Host}";
            }
            else
            {
                // Default fallback
                baseUrl = "https://localhost:7269";
            }
        }

        return $"{baseUrl}/qr/{code}";
    }
}
