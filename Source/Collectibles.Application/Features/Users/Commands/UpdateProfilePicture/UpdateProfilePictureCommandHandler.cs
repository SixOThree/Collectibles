using Collectibles.Application.Interfaces;
using Collectibles.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Features.Users.Commands.UpdateProfilePicture;

public class UpdateProfilePictureCommandHandler : IRequestHandler<UpdateProfilePictureCommand>
{
    private readonly IUserManagementService _userManagementService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<UpdateProfilePictureCommandHandler> _logger;

    public UpdateProfilePictureCommandHandler(
        IUserManagementService userManagementService,
        ICurrentUserService currentUserService,
        IFileStorage fileStorage,
        ILogger<UpdateProfilePictureCommandHandler> logger)
    {
        _userManagementService = userManagementService;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task Handle(UpdateProfilePictureCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == null)
        {
            _logger.LogWarning("Attempted to update profile picture without a UserId.");
            return;
        }

        if (_currentUserService.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to update this user's profile picture.");
        }

        if (request.FileStream == null || request.FileName == null || request.ContentType == null)
        {
            _logger.LogInformation("No profile picture provided for user {UserId}. Skipping update.", request.UserId);
            return;
        }

        _logger.LogInformation("Updating profile picture for user {UserId}", request.UserId);

        try
        {
            // Process the uploaded file
            var fileUrl = await _fileStorage.SaveFileAsync(request.FileStream, request.FileName, request.ContentType, null, cancellationToken);

            // Update the user's profile picture URL
            var result = await _userManagementService.UpdateUserProfileAsync(request.UserId, null, null, fileUrl);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Profile picture URL update failed for user {UserId}: {Errors}", request.UserId, string.Join(", ", result.Errors));
            }
            else
            {
                _logger.LogInformation("Successfully updated profile picture for user {UserId}", request.UserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile picture for user {UserId}", request.UserId);
            throw; // Re-throw to propagate the error to the UI
        }
    }
}
