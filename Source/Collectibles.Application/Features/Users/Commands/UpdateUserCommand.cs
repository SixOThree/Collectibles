using Collectibles.Application.Interfaces;
using Collectibles.Domain.Constants;
using Collectibles.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Collectibles.Application.Features.Users.Commands;

public class UpdateUserCommand : IRequest
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool SyncToolEnabled { get; set; }
}

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(v => v.DisplayName)
            .MaximumLength(100).WithMessage("Display name must not exceed 100 characters.");

        RuleFor(v => v.ProfilePictureUrl)
            .MaximumLength(500).WithMessage("Profile picture URL must not exceed 500 characters.");
    }
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand>
{
    private readonly IUserManagementService _userManagementService;
    private readonly IEventLogService _eventLogService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateUserCommandHandler(
        IUserManagementService userManagementService,
        IEventLogService eventLogService,
        ICurrentUserService currentUserService)
    {
        _userManagementService = userManagementService;
        _eventLogService = eventLogService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAdministrator && !_currentUserService.IsInRole(ApplicationConstants.Roles.UserManager))
        {
            throw new UnauthorizedAccessException("You are not authorized to update users.");
        }

        if (request.Id == _currentUserService.UserId && request.Roles.Contains(ApplicationConstants.Roles.Administrator) && !_currentUserService.IsAdministrator)
        {
            throw new UnauthorizedAccessException("You cannot grant yourself the Administrator role.");
        }

        await _userManagementService.UpdateUserAsync(
            request.Id,
            request.Email,
            request.DisplayName,
            request.ProfilePictureUrl,
            request.IsActive,
            request.Roles,
            request.SyncToolEnabled,
            cancellationToken);

        // Log the update event (without old values since we can't access UserManager here)
        var newValues = new
        {
            Email = request.Email,
            DisplayName = request.DisplayName,
            ProfilePictureUrl = request.ProfilePictureUrl,
            IsActive = request.IsActive,
            Roles = request.Roles,
            SyncToolEnabled = request.SyncToolEnabled,
        };

        await _eventLogService.LogEventAsync(
            EventAction.Update,
            "User",
            0, // We don't have numeric ID for users
            request.Email,
            null, // Can't capture old values at this layer
            newValues,
            $"User ID: {request.Id}",
            cancellationToken);
    }
}
