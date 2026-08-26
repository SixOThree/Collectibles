using Collectibles.Application.Interfaces;
using Collectibles.Domain.Constants;
using Collectibles.Domain.Entities;
using MediatR;

namespace Collectibles.Application.Features.Users.Commands;

public record DeleteUserCommand(string UserId) : IRequest;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IUserManagementService _userManagementService;
    private readonly IEventLogService _eventLogService;
    private readonly ICurrentUserService _currentUserService;

    public DeleteUserCommandHandler(
        IUserManagementService userManagementService,
        IEventLogService eventLogService,
        ICurrentUserService currentUserService)
    {
        _userManagementService = userManagementService;
        _eventLogService = eventLogService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAdministrator && !_currentUserService.IsInRole(ApplicationConstants.Roles.UserManager))
        {
            throw new UnauthorizedAccessException("You are not authorized to delete users.");
        }

        if (request.UserId == _currentUserService.UserId)
        {
            throw new UnauthorizedAccessException("You cannot delete your own account.");
        }

        await _userManagementService.DeleteUserAsync(request.UserId, cancellationToken);

        // Log the delete event
        await _eventLogService.LogEventAsync(
            EventAction.Delete,
            "User",
            0, // We don't have numeric ID for users
            "User",
            null, // Can't capture user info at this layer
            null,
            $"User ID: {request.UserId}",
            cancellationToken);
    }
}
