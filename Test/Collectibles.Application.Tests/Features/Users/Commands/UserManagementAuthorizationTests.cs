using Collectibles.Application.Features.Users.Commands;
using Collectibles.Application.Features.Users.Dtos;
using Collectibles.Application.Features.Users.Queries;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Constants;
using Moq;

namespace Collectibles.Application.Tests.Features.Users.Commands;

public class UserManagementAuthorizationTests
{
    private readonly Mock<IUserManagementService> _userManagementServiceMock = new();
    private readonly Mock<IEventLogService> _eventLogServiceMock = new();

    private static Mock<ICurrentUserService> CreateCurrentUser(string? userId = "user-1", bool isAdmin = false, bool isUserManager = false)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(x => x.UserId).Returns(userId);
        mock.Setup(x => x.IsAdministrator).Returns(isAdmin);
        mock.Setup(x => x.IsInRole(ApplicationConstants.Roles.UserManager)).Returns(isUserManager);
        return mock;
    }

    [Fact]
    public async Task CreateUserShouldThrowWhenNotAdministrator()
    {
        var handler = new CreateUserCommandHandler(
            _userManagementServiceMock.Object,
            _eventLogServiceMock.Object,
            CreateCurrentUser(isAdmin: false).Object);

        var act = async () => await handler.Handle(new CreateUserCommand
        {
            Email = "new@example.com",
            Password = "password123",
            Roles = new List<string> { ApplicationConstants.Roles.Viewer },
        }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _userManagementServiceMock.Verify(
            x => x.CreateUserAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateUserShouldSucceedWhenAdministrator()
    {
        _userManagementServiceMock
            .Setup(x => x.CreateUserAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-user-id");

        var handler = new CreateUserCommandHandler(
            _userManagementServiceMock.Object,
            _eventLogServiceMock.Object,
            CreateCurrentUser(isAdmin: true).Object);

        var result = await handler.Handle(new CreateUserCommand
        {
            Email = "new@example.com",
            Password = "password123",
            Roles = new List<string> { ApplicationConstants.Roles.Viewer },
        }, CancellationToken.None);

        result.Should().Be("new-user-id");
    }

    [Fact]
    public async Task UpdateUserShouldThrowWhenNotAuthorized()
    {
        var handler = new UpdateUserCommandHandler(
            _userManagementServiceMock.Object,
            _eventLogServiceMock.Object,
            CreateCurrentUser(isAdmin: false, isUserManager: false).Object);

        var act = async () => await handler.Handle(new UpdateUserCommand
        {
            Id = "other-user",
            Email = "other@example.com",
            Roles = new List<string>(),
        }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task UpdateUserShouldSucceedWhenUserManager()
    {
        var handler = new UpdateUserCommandHandler(
            _userManagementServiceMock.Object,
            _eventLogServiceMock.Object,
            CreateCurrentUser(isAdmin: false, isUserManager: true).Object);

        await handler.Handle(new UpdateUserCommand
        {
            Id = "other-user",
            Email = "other@example.com",
            Roles = new List<string> { ApplicationConstants.Roles.Viewer },
        }, CancellationToken.None);

        _userManagementServiceMock.Verify(
            x => x.UpdateUserAsync(
                "other-user",
                "other@example.com",
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<List<string>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserShouldThrowWhenUserManagerGrantsSelfAdministrator()
    {
        var handler = new UpdateUserCommandHandler(
            _userManagementServiceMock.Object,
            _eventLogServiceMock.Object,
            CreateCurrentUser(userId: "user-1", isAdmin: false, isUserManager: true).Object);

        var act = async () => await handler.Handle(new UpdateUserCommand
        {
            Id = "user-1",
            Email = "self@example.com",
            Roles = new List<string> { ApplicationConstants.Roles.Administrator },
        }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You cannot grant yourself the Administrator role.");
        _userManagementServiceMock.Verify(
            x => x.UpdateUserAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<List<string>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateUserShouldSucceedWhenAdministratorGrantsSelfAdministrator()
    {
        var handler = new UpdateUserCommandHandler(
            _userManagementServiceMock.Object,
            _eventLogServiceMock.Object,
            CreateCurrentUser(userId: "user-1", isAdmin: true).Object);

        await handler.Handle(new UpdateUserCommand
        {
            Id = "user-1",
            Email = "self@example.com",
            Roles = new List<string> { ApplicationConstants.Roles.Administrator },
        }, CancellationToken.None);

        _userManagementServiceMock.Verify(
            x => x.UpdateUserAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<List<string>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteUserShouldThrowWhenNotAuthorized()
    {
        var handler = new DeleteUserCommandHandler(
            _userManagementServiceMock.Object,
            _eventLogServiceMock.Object,
            CreateCurrentUser(isAdmin: false, isUserManager: false).Object);

        var act = async () => await handler.Handle(new DeleteUserCommand("other-user"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task DeleteUserShouldThrowWhenDeletingSelf()
    {
        var handler = new DeleteUserCommandHandler(
            _userManagementServiceMock.Object,
            _eventLogServiceMock.Object,
            CreateCurrentUser(userId: "user-1", isAdmin: true).Object);

        var act = async () => await handler.Handle(new DeleteUserCommand("user-1"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You cannot delete your own account.");
        _userManagementServiceMock.Verify(
            x => x.DeleteUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteUserShouldSucceedWhenAdministratorDeletesOtherUser()
    {
        var handler = new DeleteUserCommandHandler(
            _userManagementServiceMock.Object,
            _eventLogServiceMock.Object,
            CreateCurrentUser(userId: "user-1", isAdmin: true).Object);

        await handler.Handle(new DeleteUserCommand("other-user"), CancellationToken.None);

        _userManagementServiceMock.Verify(
            x => x.DeleteUserAsync("other-user", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LockUnlockUserShouldThrowWhenNotAuthorized()
    {
        var handler = new LockUnlockUserCommandHandler(
            _userManagementServiceMock.Object,
            CreateCurrentUser(isAdmin: false, isUserManager: false).Object);

        var act = async () => await handler.Handle(new LockUnlockUserCommand
        {
            UserId = "other-user",
            IsLocked = true,
        }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task LockUnlockUserShouldThrowWhenLockingSelf()
    {
        var handler = new LockUnlockUserCommandHandler(
            _userManagementServiceMock.Object,
            CreateCurrentUser(userId: "user-1", isAdmin: true).Object);

        var act = async () => await handler.Handle(new LockUnlockUserCommand
        {
            UserId = "user-1",
            IsLocked = true,
        }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You cannot lock or unlock your own account.");
    }

    [Fact]
    public async Task GetUsersListShouldThrowWhenNotAuthorized()
    {
        var handler = new GetUsersListQueryHandler(
            _userManagementServiceMock.Object,
            CreateCurrentUser(isAdmin: false, isUserManager: false).Object);

        var act = async () => await handler.Handle(new GetUsersListQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetUserByIdShouldThrowWhenViewingOtherUserWithoutPermission()
    {
        var handler = new GetUserByIdQueryHandler(
            _userManagementServiceMock.Object,
            CreateCurrentUser(userId: "user-1", isAdmin: false, isUserManager: false).Object);

        var act = async () => await handler.Handle(new GetUserByIdQuery("other-user"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetUserByIdShouldSucceedWhenViewingSelf()
    {
        _userManagementServiceMock
            .Setup(x => x.GetUserByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserDto { Id = "user-1" });

        var handler = new GetUserByIdQueryHandler(
            _userManagementServiceMock.Object,
            CreateCurrentUser(userId: "user-1", isAdmin: false, isUserManager: false).Object);

        var result = await handler.Handle(new GetUserByIdQuery("user-1"), CancellationToken.None);

        result.Id.Should().Be("user-1");
    }

    [Fact]
    public async Task GetUserByIdShouldSucceedWhenUserManagerViewsOtherUser()
    {
        _userManagementServiceMock
            .Setup(x => x.GetUserByIdAsync("other-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserDto { Id = "other-user" });

        var handler = new GetUserByIdQueryHandler(
            _userManagementServiceMock.Object,
            CreateCurrentUser(userId: "user-1", isAdmin: false, isUserManager: true).Object);

        var result = await handler.Handle(new GetUserByIdQuery("other-user"), CancellationToken.None);

        result.Id.Should().Be("other-user");
    }
}
