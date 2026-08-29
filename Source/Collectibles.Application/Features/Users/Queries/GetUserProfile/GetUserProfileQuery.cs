using Collectibles.Application.Features.Users.Dtos;

using MediatR;

namespace Collectibles.Application.Features.Users.Queries.GetUserProfile;

public class GetUserProfileQuery : IRequest<UserProfileDto>
{
    public string? UserId { get; set; }
}
