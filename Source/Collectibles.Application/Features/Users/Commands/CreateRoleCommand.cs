using Collectibles.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace Collectibles.Application.Features.Users.Commands;

public record CreateRoleCommand(string RoleName) : IRequest<string>;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(v => v.RoleName)
            .NotEmpty().WithMessage("Role name is required.")
            .MinimumLength(2).WithMessage("Role name must be at least 2 characters.")
            .MaximumLength(256).WithMessage("Role name must not exceed 256 characters.")
            .Matches("^[a-zA-Z0-9._-]+$").WithMessage("Role name can only contain letters, numbers, and ._- characters.");
    }
}

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, string>
{
    private readonly IUserManagementService _userManagementService;

    public CreateRoleCommandHandler(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task<string> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        return await _userManagementService.CreateRoleAsync(request.RoleName, cancellationToken);
    }
}
