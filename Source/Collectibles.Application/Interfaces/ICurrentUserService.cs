namespace Collectibles.Application.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsAdministrator { get; }
}
