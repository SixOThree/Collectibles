using Collectibles.Application.Interfaces;

namespace Collectibles.Infrastructure.Services;

public class SessionTrackingService : ISessionTrackingService
{
    private string? _trackingId;

    public string? TrackingId
    {
        get
        {
            if (string.IsNullOrEmpty(_trackingId))
            {
                _trackingId = Guid.NewGuid().ToString("N");
            }

            return _trackingId;
        }
        private set => _trackingId = value;
    }

    public string? SessionId => TrackingId != null ? $"session_{TrackingId}" : null;

    public void SetTrackingId(string trackingId)
    {
        if (!string.IsNullOrEmpty(trackingId))
        {
            _trackingId = trackingId;
        }
    }
}
