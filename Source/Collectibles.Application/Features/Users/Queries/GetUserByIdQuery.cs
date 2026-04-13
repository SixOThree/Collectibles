using Collectibles.Application.Features.Users.Dtos;
using Collectibles.Application.Interfaces;
using MediatR;

namespace Collectibles.Application.Features.Users.Queries;

public record GetUserByIdQuery(string UserId) : IRequest<UserDto>;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserManagementService _userManagementService;

    public GetUserByIdQueryHandler(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        return await _userManagementService.GetUserByIdAsync(request.UserId, cancellationToken);
    }
}
