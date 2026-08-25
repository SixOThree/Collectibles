using Collectibles.Application.Features.Attachments.Commands;
using Collectibles.Application.Features.CollectibleItems.Commands;
using Collectibles.Application.Features.ContentDefinitions.Commands;
using Collectibles.Application.Features.Maintenance.Commands;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Collectibles.Application.Tests.Features.Maintenance;

public class MaintenanceCommandAuthorizationTests
{
    private static Mock<ICurrentUserService> CreateCurrentUser(bool isAdmin)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(x => x.IsAdministrator).Returns(isAdmin);
        return mock;
    }

    private static IApplicationDbContextFactory CreateContextFactory(ApplicationDbContext context)
    {
        var mock = new Mock<IApplicationDbContextFactory>();
        mock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);
        mock.Setup(x => x.CreateDbContext())
            .Returns(context);
        return mock.Object;
    }

    [Fact]
    public async Task UpdateMissingPreviewImagesShouldThrowWhenNotAdministrator()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options, CreateCurrentUser(false).Object);

        var handler = new UpdateMissingPreviewImagesCommandHandler(
            CreateContextFactory(context),
            new Mock<IEventLogService>().Object,
            new Mock<ILogger<UpdateMissingPreviewImagesCommandHandler>>().Object,
            CreateCurrentUser(false).Object);

        var act = async () => await handler.Handle(new UpdateMissingPreviewImagesCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RollbackAttachmentMigrationShouldThrowWhenNotAdministrator()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options, CreateCurrentUser(false).Object);
        context.Database.EnsureCreated();

        var handler = new RollbackAttachmentMigrationCommandHandler(
            context,
            new Mock<IFileStorage>().Object,
            new Mock<ILogger<RollbackAttachmentMigrationCommandHandler>>().Object,
            CreateCurrentUser(false).Object);

        var act = async () => await handler.Handle(new RollbackAttachmentMigrationCommand
        {
            AttachmentIds = new List<long>(),
        }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CleanupMigratedAttachmentsShouldThrowWhenNotAdministrator()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options, CreateCurrentUser(false).Object);

        var handler = new CleanupMigratedAttachmentsCommandHandler(
            CreateContextFactory(context),
            new Mock<ILogger<CleanupMigratedAttachmentsCommandHandler>>().Object,
            CreateCurrentUser(false).Object);

        var act = async () => await handler.Handle(new CleanupMigratedAttachmentsCommand
        {
            RetentionDays = 30,
        }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CleanupOrphansShouldThrowWhenNotAdministrator()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options, CreateCurrentUser(false).Object);

        var handler = new CleanupOrphansCommandHandler(
            CreateContextFactory(context),
            new Mock<IFileStorage>().Object,
            new Mock<IEventLogService>().Object,
            CreateCurrentUser(false).Object);

        var act = async () => await handler.Handle(new CleanupOrphansCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task SetDefaultContentDefinitionShouldThrowWhenNotAdministrator()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options, CreateCurrentUser(false).Object);
        context.Database.EnsureCreated();

        var handler = new SetDefaultContentDefinitionCommandHandler(
            context,
            CreateCurrentUser(false).Object);

        var act = async () => await handler.Handle(new SetDefaultContentDefinitionCommand { Id = 1L }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
