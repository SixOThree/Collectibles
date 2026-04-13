using Collectibles.Application.Common.Models;
using Collectibles.Application.Features.Users.Dtos;
using Collectibles.Application.Interfaces;
using MediatR;

namespace Collectibles.Application.Features.Users.Queries;

public class GetUsersListQuery : IRequest<PaginatedList<UserListDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? RoleFilter { get; set; }
    public bool? ActiveFilter { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}

public class GetUsersListQueryHandler : IRequestHandler<GetUsersListQuery, PaginatedList<UserListDto>>
{
    private readonly IUserManagementService _userManagementService;

    public GetUsersListQueryHandler(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task<PaginatedList<UserListDto>> Handle(GetUsersListQuery request, CancellationToken cancellationToken)
    {
        return await _userManagementService.GetUsersAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.RoleFilter,
            request.ActiveFilter,
            request.SortBy,
            request.SortDescending,
            cancellationToken);
    }
}
