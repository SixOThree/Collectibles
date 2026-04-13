using MediatR;

namespace Collectibles.Application.Features.Users.Commands.UpdateUserProfile;

public class UpdateUserProfileCommand : IRequest
{
    public string? UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
