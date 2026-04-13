using Collectibles.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace Collectibles.Application.Features.Users.Commands;

public class ChangeUserPasswordCommand : IRequest
{
    public string UserId { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public bool RequirePasswordChange { get; set; }
}

public class ChangeUserPasswordCommandValidator : AbstractValidator<ChangeUserPasswordCommand>
{
    public ChangeUserPasswordCommandValidator()
    {
        RuleFor(v => v.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(v => v.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
    }
}

public class ChangeUserPasswordCommandHandler : IRequestHandler<ChangeUserPasswordCommand>
{
    private readonly IUserManagementService _userManagementService;
    private readonly ICurrentUserService _currentUserService;

    public ChangeUserPasswordCommandHandler(IUserManagementService userManagementService, ICurrentUserService currentUserService)
    {
        _userManagementService = userManagementService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(ChangeUserPasswordCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to change this user's password.");
        }

        await _userManagementService.ChangeUserPasswordAsync(
            request.UserId,
            request.NewPassword,
            request.RequirePasswordChange,
            cancellationToken);
    }
}
