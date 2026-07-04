namespace Collectibles.Application.Interfaces;

public interface ISessionTrackingService
{
    string? TrackingId { get; }
    string? SessionId { get; }
    void SetTrackingId(string trackingId);
}
