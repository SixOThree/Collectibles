using Collectibles.Application.Features.Users.Dtos;
using Collectibles.Application.Interfaces;

using MediatR;

namespace Collectibles.Application.Features.Users.Queries;

public record GetRolesListQuery : IRequest<List<RoleDto>>;

public class GetRolesListQueryHandler : IRequestHandler<GetRolesListQuery, List<RoleDto>>
{
    private readonly IUserManagementService _userManagementService;

    public GetRolesListQueryHandler(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task<List<RoleDto>> Handle(GetRolesListQuery request, CancellationToken cancellationToken)
    {
        return await _userManagementService.GetRolesAsync(cancellationToken);
    }
}
