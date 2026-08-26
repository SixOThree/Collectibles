using Collectibles.Application.Features.ContentDefinitions.Commands;
using Collectibles.Application.Interfaces;
using Moq;

namespace Collectibles.Application.Tests.Features.ContentDefinitions.Commands;

public class SetDefaultContentDefinitionCommandAuthorizationTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

    private SetDefaultContentDefinitionCommandHandler CreateHandler()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options, Mock.Of<ICurrentUserService>());
        context.Database.EnsureCreated();

        return new SetDefaultContentDefinitionCommandHandler(
            context,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task HandleShouldThrowWhenNotAdministrator()
    {
        _currentUserServiceMock.Setup(x => x.IsAdministrator).Returns(false);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new SetDefaultContentDefinitionCommand { Id = 1L }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task HandleShouldSucceedWhenAdministrator()
    {
        _currentUserServiceMock.Setup(x => x.IsAdministrator).Returns(true);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new SetDefaultContentDefinitionCommand { Id = 1L }, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
