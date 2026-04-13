using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using MediatR;

namespace Collectibles.Application.Features.Users.Commands;

public record DeleteUserCommand(string UserId) : IRequest;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IUserManagementService _userManagementService;
    private readonly IEventLogService _eventLogService;

    public DeleteUserCommandHandler(
        IUserManagementService userManagementService,
        IEventLogService eventLogService)
    {
        _userManagementService = userManagementService;
        _eventLogService = eventLogService;
    }

    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
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
