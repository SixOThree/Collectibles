using MediatR;

namespace Collectibles.Application.Features.Users.Commands.UpdateProfilePicture;

public class UpdateProfilePictureCommand : IRequest
{
    public string? UserId { get; set; }
    public Stream? FileStream { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
}
