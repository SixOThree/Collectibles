using Collectibles.Application.Interfaces;
using MediatR;

namespace Collectibles.Application.Features.Users.Commands;

public record DeleteRoleCommand(string RoleId) : IRequest;

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand>
{
    private readonly IUserManagementService _userManagementService;

    public DeleteRoleCommandHandler(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        await _userManagementService.DeleteRoleAsync(request.RoleId, cancellationToken);
    }
}
