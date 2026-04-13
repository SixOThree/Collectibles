using Collectibles.Application.Features.Attachments.Commands;
using Collectibles.Application.Tests.Common;
using Collectibles.Domain.Configuration.Storage;
using Collectibles.Domain.Enums;
using Collectibles.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Collectibles.Application.Tests.Features.Attachments.Commands;

public class UpdateAttachmentCommandAuthorizationTests : BaseTestFixture
{
    private readonly Mock<IApplicationDbContextFactory> _contextFactoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    public UpdateAttachmentCommandAuthorizationTests()
    {
        _contextFactoryMock = new Mock<IApplicationDbContextFactory>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns("test-user-id");

        _contextFactoryMock
            .Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new NonDisposableDbContextWrapper(Context as IApplicationDbContext));
    }

    private UpdateAttachmentCommandHandler CreateHandler()
    {
        var storageSettings = Options.Create(new StorageSettings { Provider = StorageProvider.Database });
        return new UpdateAttachmentCommandHandler(
            _contextFactoryMock.Object,
            Mock.Of<IFileStorage>(),
            Mock.Of<IFileProcessingService>(),
            Mock.Of<IEventLogService>(),
            storageSettings,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task HandleOtherUsersAttachmentShouldThrowUnauthorizedAccessException()
    {
        // Create showcase owned by another user
        var showcase = new Showcase
        {
            Name = "Other User Showcase",
            UserId = "other-user-id",
        };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        // Create collectible item in that showcase
        var collectibleItem = new CollectibleItem
        {
            Name = "Other User Item",
        };
        collectibleItem.Showcases.Add(showcase);
        Context.CollectibleItems.Add(collectibleItem);
        await Context.SaveChangesAsync();

        // Create attachment owned by the other user
        var attachment = new Attachment
        {
            Name = "Other User Attachment",
            CreatedBy = "other-user-id",
        };
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        // Link attachment to item
        var link = new CollectibleItemAttachment
        {
            CollectibleItemId = collectibleItem.Id,
            AttachmentId = attachment.Id,
        };
        Context.CollectibleItemAttachments.Add(link);
        await Context.SaveChangesAsync();

        var command = new UpdateAttachmentCommand
        {
            Id = attachment.Id,
            Name = "Attempted Update",
        };

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You are not authorized to update this attachment.");
    }

    [Fact]
    public async Task HandleOwnShowcaseAttachmentShouldSucceed()
    {
        // Create showcase owned by the current user
        var showcase = new Showcase
        {
            Name = "My Showcase",
            UserId = "test-user-id",
        };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        // Create collectible item in that showcase
        var collectibleItem = new CollectibleItem
        {
            Name = "My Item",
        };
        collectibleItem.Showcases.Add(showcase);
        Context.CollectibleItems.Add(collectibleItem);
        await Context.SaveChangesAsync();

        // Create attachment (CreatedBy will be set to "test-user-id" by DbContext)
        var attachment = new Attachment
        {
            Name = "My Attachment",
        };
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        // Link attachment to item
        var link = new CollectibleItemAttachment
        {
            CollectibleItemId = collectibleItem.Id,
            AttachmentId = attachment.Id,
        };
        Context.CollectibleItemAttachments.Add(link);
        await Context.SaveChangesAsync();

        var command = new UpdateAttachmentCommand
        {
            Id = attachment.Id,
            Name = "Updated Name",
        };

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken);

        await act.Should().NotThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task HandleOrphanAttachmentCreatedByCurrentUserShouldSucceed()
    {
        // Create attachment not linked to any item (orphan), but created by current user
        var attachment = new Attachment
        {
            Name = "Orphan Attachment",
            // CreatedBy will be set to "test-user-id" by DbContext
        };
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        var command = new UpdateAttachmentCommand
        {
            Id = attachment.Id,
            Name = "Updated Orphan",
        };

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken);

        await act.Should().NotThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task HandleOrphanAttachmentCreatedByOtherUserShouldThrowUnauthorizedAccessException()
    {
        // Create attachment not linked to any item, created by another user
        var attachment = new Attachment
        {
            Name = "Other Orphan Attachment",
            CreatedBy = "other-user-id",
        };
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        var command = new UpdateAttachmentCommand
        {
            Id = attachment.Id,
            Name = "Attempted Update",
        };

        var handler = CreateHandler();
        var act = async () => await handler.Handle(command, CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You are not authorized to update this attachment.");
    }
}
