using Collectibles.Application.Features.Attachments.Commands;
using Collectibles.Application.Features.CollectibleItems.Commands;
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

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options, Mock.Of<ICurrentUserService>());
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task UpdateMissingPreviewImagesShouldThrowWhenNotAdministrator()
    {
        var handler = new UpdateMissingPreviewImagesCommandHandler(
            CreateContextFactory(CreateContext()),
            new Mock<IEventLogService>().Object,
            new Mock<ILogger<UpdateMissingPreviewImagesCommandHandler>>().Object,
            CreateCurrentUser(false).Object);

        var act = async () => await handler.Handle(new UpdateMissingPreviewImagesCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RollbackAttachmentMigrationShouldThrowWhenNotAdministrator()
    {
        var handler = new RollbackAttachmentMigrationCommandHandler(
            CreateContext(),
            new Mock<IFileStorage>().Object,
            new Mock<ILogger<RollbackAttachmentMigrationCommandHandler>>().Object,
            CreateCurrentUser(false).Object);

        var act = async () => await handler.Handle(
            new RollbackAttachmentMigrationCommand
            {
                AttachmentIds = new List<long>(),
            }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CleanupMigratedAttachmentsShouldThrowWhenNotAdministrator()
    {
        var handler = new CleanupMigratedAttachmentsCommandHandler(
            CreateContextFactory(CreateContext()),
            new Mock<ILogger<CleanupMigratedAttachmentsCommandHandler>>().Object,
            CreateCurrentUser(false).Object);

        var act = async () => await handler.Handle(
            new CleanupMigratedAttachmentsCommand
            {
                RetentionDays = 30,
            }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CleanupOrphansShouldThrowWhenNotAdministrator()
    {
        var handler = new CleanupOrphansCommandHandler(
            CreateContextFactory(CreateContext()),
            new Mock<IFileStorage>().Object,
            new Mock<IEventLogService>().Object,
            CreateCurrentUser(false).Object);

        var act = async () => await handler.Handle(new CleanupOrphansCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
