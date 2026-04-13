using Collectibles.Application.Common.Models;
using Collectibles.Application.Features.Users.Commands.UpdateUserProfile;
using Microsoft.Extensions.Logging.Abstractions;

namespace Collectibles.Application.Tests.Features.Users.Commands;

public class UpdateUserProfileCommandTests
{
    private readonly Mock<IUserManagementService> _userManagementServiceMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    public UpdateUserProfileCommandTests()
    {
        _userManagementServiceMock = new Mock<IUserManagementService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _currentUserServiceMock
            .Setup(x => x.UserId)
            .Returns("test-user-id");

        _userManagementServiceMock
            .Setup(x => x.UpdateUserProfileAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), null))
            .ReturnsAsync(Result.Success());
    }

    private UpdateUserProfileCommandHandler CreateHandler()
    {
        return new UpdateUserProfileCommandHandler(
            _userManagementServiceMock.Object,
            _currentUserServiceMock.Object,
            NullLogger<UpdateUserProfileCommandHandler>.Instance);
    }

    [Fact]
    public async Task HandleOwnUserShouldUpdateProfile()
    {
        var command = new UpdateUserProfileCommand
        {
            UserId = "test-user-id",
            FirstName = "John",
            LastName = "Doe",
        };

        var handler = CreateHandler();
        await handler.Handle(command, CancellationToken.None);

        _userManagementServiceMock.Verify(
            x => x.UpdateUserProfileAsync("test-user-id", "John", "Doe", null),
            Times.Once);
    }

    [Fact]
    public async Task HandleOtherUserShouldThrowUnauthorizedAccessException()
    {
        var command = new UpdateUserProfileCommand
        {
            UserId = "other-user-id",
            FirstName = "John",
            LastName = "Doe",
        };

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You are not authorized to update this user's profile.");
    }
}
