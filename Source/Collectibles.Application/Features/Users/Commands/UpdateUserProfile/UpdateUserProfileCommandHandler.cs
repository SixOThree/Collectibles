using Collectibles.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Features.Users.Commands.UpdateUserProfile;

public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand>
{
    private readonly IUserManagementService _userManagementService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateUserProfileCommandHandler> _logger;

    public UpdateUserProfileCommandHandler(
        IUserManagementService userManagementService,
        ICurrentUserService currentUserService,
        ILogger<UpdateUserProfileCommandHandler> logger)
    {
        _userManagementService = userManagementService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to update this user's profile.");
        }

        _logger.LogInformation("Updating profile for user {UserId}", request.UserId);

        var result = await _userManagementService.UpdateUserProfileAsync(request.UserId, request.FirstName, request.LastName, null);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Profile update failed for user {UserId}: {Errors}", request.UserId, string.Join(", ", result.Errors));

            // Optionally, you can throw an exception here to propagate the error.
        }
        else
        {
            _logger.LogInformation("Successfully updated profile for user {UserId}", request.UserId);
        }
    }
}
