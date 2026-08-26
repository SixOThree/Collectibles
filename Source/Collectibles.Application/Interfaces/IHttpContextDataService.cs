namespace Collectibles.Application.Interfaces;

/// <summary>
/// Service for accessing HTTP context data that was captured during the initial HTTP request.
/// This is safe to use in Blazor Server Interactive components.
/// </summary>
public interface IHttpContextDataService
{
    bool IsInitialized { get; }
    bool IsAuthenticated { get; }
    bool IsAdministrator { get; }
    string? UserId { get; }
    string? UserName { get; }
    string? UserAgent { get; }
    string? ClientIpAddress { get; }
    string? RequestPath { get; }
    List<string> UserRoles { get; }
}
