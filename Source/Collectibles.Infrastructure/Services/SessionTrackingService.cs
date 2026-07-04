using Collectibles.Application.Interfaces;

namespace Collectibles.Infrastructure.Services;

public class SessionTrackingService : ISessionTrackingService
{
    public string? TrackingId { get; private set; }

    public string? SessionId => TrackingId != null ? $"session_{TrackingId}" : null;

    public void SetTrackingId(string trackingId)
    {
        if (string.IsNullOrEmpty(TrackingId))
        {
            TrackingId = trackingId;
        }
    }
}
