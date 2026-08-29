using Collectibles.Application.Features.Users.Dtos;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Constants;

using MediatR;

namespace Collectibles.Application.Features.Users.Queries;

public record GetUserByIdQuery(string UserId) : IRequest<UserDto>;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserManagementService _userManagementService;
    private readonly ICurrentUserService _currentUserService;

    public GetUserByIdQueryHandler(IUserManagementService userManagementService, ICurrentUserService currentUserService)
    {
        _userManagementService = userManagementService;
        _currentUserService = currentUserService;
    }

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        if (request.UserId != _currentUserService.UserId
            && !_currentUserService.IsAdministrator
            && !_currentUserService.IsInRole(ApplicationConstants.Roles.UserManager))
        {
            throw new UnauthorizedAccessException("You are not authorized to view this user.");
        }

        return await _userManagementService.GetUserByIdAsync(request.UserId, cancellationToken);
    }
}
