using Collectibles.Application.Features.Showcases;
using Collectibles.Application.Features.Showcases.Queries;
using Collectibles.Application.Mappings.Explicit;
using Collectibles.Application.Tests.Common;

namespace Collectibles.Application.Tests.Features.Showcases.Queries;

public class GetShowcaseByIdQueryAuthorizationTests : BaseTestFixture
{
    private readonly Mock<IApplicationDbContextFactory> _contextFactoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    public GetShowcaseByIdQueryAuthorizationTests()
    {
        _contextFactoryMock = new Mock<IApplicationDbContextFactory>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns("test-user-id");

        _contextFactoryMock
            .Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new NonDisposableDbContextWrapper(Context as IApplicationDbContext));
    }

    [Fact]
    public async Task Handle_PrivateShowcaseOwnedByOtherUser_ReturnsNull()
    {
        // Arrange
        var showcase = new Showcase
        {
            Name = "Private",
            UserId = "other-user-id",
            IsPrivate = true,
        };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        var handler = new GetShowcaseByIdQueryHandler(
            _contextFactoryMock.Object,
            Mock.Of<IEventLogService>(),
            Mock.Of<IShowcaseMappingService>(),
            _currentUserServiceMock.Object);

        // Act
        var result = await handler.Handle(new GetShowcaseByIdQuery(showcase.Id), CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_PrivateShowcaseOwnedByOtherUser_AdminCanAccess()
    {
        // Arrange
        var adminUserServiceMock = new Mock<ICurrentUserService>();
        adminUserServiceMock.Setup(x => x.UserId).Returns("admin-user-id");
        adminUserServiceMock.Setup(x => x.IsAdministrator).Returns(true);

        var showcase = new Showcase
        {
            Name = "Private",
            UserId = "other-user-id",
            IsPrivate = true,
        };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        var mappingMock = new Mock<IShowcaseMappingService>();
        mappingMock.Setup(x => x.MapToDetailDtoAsync(It.IsAny<Showcase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShowcaseDetailDto { Id = showcase.Id, Name = showcase.Name });

        var handler = new GetShowcaseByIdQueryHandler(
            _contextFactoryMock.Object,
            Mock.Of<IEventLogService>(),
            mappingMock.Object,
            adminUserServiceMock.Object);

        // Act
        var result = await handler.Handle(new GetShowcaseByIdQuery(showcase.Id), CancellationToken);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_PublicShowcaseOwnedByOtherUser_ReturnsData()
    {
        // Arrange
        var showcase = new Showcase
        {
            Name = "Public",
            UserId = "other-user-id",
            IsPrivate = false,
        };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        var mappingMock = new Mock<IShowcaseMappingService>();
        mappingMock.Setup(x => x.MapToDetailDtoAsync(It.IsAny<Showcase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShowcaseDetailDto { Id = showcase.Id, Name = showcase.Name });

        var handler = new GetShowcaseByIdQueryHandler(
            _contextFactoryMock.Object,
            Mock.Of<IEventLogService>(),
            mappingMock.Object,
            _currentUserServiceMock.Object);

        // Act
        var result = await handler.Handle(new GetShowcaseByIdQuery(showcase.Id), CancellationToken);

        // Assert
        result.Should().NotBeNull();
    }
}
