using Collectibles.Application.Interfaces;
using Collectibles.Domain.Constants;
using MediatR;

namespace Collectibles.Application.Features.Users.Commands;

public class LockUnlockUserCommand : IRequest
{
    public string UserId { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public int? LockoutDays { get; set; }
}

public class LockUnlockUserCommandHandler : IRequestHandler<LockUnlockUserCommand>
{
    private readonly IUserManagementService _userManagementService;
    private readonly ICurrentUserService _currentUserService;

    public LockUnlockUserCommandHandler(IUserManagementService userManagementService, ICurrentUserService currentUserService)
    {
        _userManagementService = userManagementService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(LockUnlockUserCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAdministrator && !_currentUserService.IsInRole(ApplicationConstants.Roles.UserManager))
        {
            throw new UnauthorizedAccessException("You are not authorized to lock or unlock users.");
        }

        if (request.UserId == _currentUserService.UserId)
        {
            throw new UnauthorizedAccessException("You cannot lock or unlock your own account.");
        }

        await _userManagementService.LockUnlockUserAsync(
            request.UserId,
            request.IsLocked,
            request.LockoutDays,
            cancellationToken);
    }
}
