using Collectibles.Application.Interfaces;
using Collectibles.Domain.Constants;
using Collectibles.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Collectibles.Application.Features.Users.Commands;

public class CreateUserCommand : IRequest<string>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
    public List<string> Roles { get; set; } = new();
}

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");

        RuleFor(v => v.DisplayName)
            .MaximumLength(100).WithMessage("Display name must not exceed 100 characters.");
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, string>
{
    private readonly IUserManagementService _userManagementService;
    private readonly IEventLogService _eventLogService;
    private readonly ICurrentUserService _currentUserService;

    public CreateUserCommandHandler(
        IUserManagementService userManagementService,
        IEventLogService eventLogService,
        ICurrentUserService currentUserService)
    {
        _userManagementService = userManagementService;
        _eventLogService = eventLogService;
        _currentUserService = currentUserService;
    }

    public async Task<string> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAdministrator)
        {
            throw new UnauthorizedAccessException("Only administrators can create users.");
        }

        var userId = await _userManagementService.CreateUserAsync(
            request.Email,
            request.Password,
            request.DisplayName,
            request.IsActive,
            request.Roles,
            cancellationToken);

        // Log the creation event
        await _eventLogService.LogEventAsync(
            EventAction.Create,
            "User",
            0, // We don't have numeric ID for users
            request.Email,
            null,
            new
            {
                Email = request.Email,
                DisplayName = request.DisplayName,
                IsActive = request.IsActive,
                Roles = request.Roles,
                UserId = userId,
            },
            $"User ID: {userId}",
            cancellationToken);

        return userId;
    }
}
