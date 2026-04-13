using Collectibles.Application.Features.Users.Commands;

namespace Collectibles.Application.Tests.Features.Users.Commands;

public class ChangeUserPasswordCommandTests
{
    private readonly Mock<IUserManagementService> _userManagementServiceMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    public ChangeUserPasswordCommandTests()
    {
        _userManagementServiceMock = new Mock<IUserManagementService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _currentUserServiceMock
            .Setup(x => x.UserId)
            .Returns("test-user-id");
    }

    private ChangeUserPasswordCommandHandler CreateHandler()
    {
        return new ChangeUserPasswordCommandHandler(
            _userManagementServiceMock.Object,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task HandleOwnUserShouldChangePassword()
    {
        var command = new ChangeUserPasswordCommand
        {
            UserId = "test-user-id",
            NewPassword = "NewPassword123!",
            RequirePasswordChange = false,
        };

        var handler = CreateHandler();
        await handler.Handle(command, CancellationToken.None);

        _userManagementServiceMock.Verify(
            x => x.ChangeUserPasswordAsync("test-user-id", "NewPassword123!", false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleOtherUserShouldThrowUnauthorizedAccessException()
    {
        var command = new ChangeUserPasswordCommand
        {
            UserId = "other-user-id",
            NewPassword = "NewPassword123!",
            RequirePasswordChange = false,
        };

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You are not authorized to change this user's password.");
    }
}
