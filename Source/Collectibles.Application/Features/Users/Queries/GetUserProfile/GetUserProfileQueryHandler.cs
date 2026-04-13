using Collectibles.Application.Features.Users.Dtos;
using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;
using MediatR;

namespace Collectibles.Application.Features.Users.Queries.GetUserProfile;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly IUserManagementService _userManagementService;

    public GetUserProfileQueryHandler(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.UserId))
        {
            throw new ArgumentNullException(nameof(request.UserId), "UserId cannot be null or empty");
        }

        var user = await _userManagementService.GetUserByIdAsync(request.UserId, cancellationToken);
        return user.ToProfileDto();
    }
}
