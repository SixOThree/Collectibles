using Collectibles.Application.Interfaces;
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

    public LockUnlockUserCommandHandler(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task Handle(LockUnlockUserCommand request, CancellationToken cancellationToken)
    {
        await _userManagementService.LockUnlockUserAsync(
            request.UserId,
            request.IsLocked,
            request.LockoutDays,
            cancellationToken);
    }
}
