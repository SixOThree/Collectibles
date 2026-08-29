using Collectibles.Application.Features.Attachments.Commands;
using Collectibles.Application.Features.Sync.Commands;
using Collectibles.Application.Features.Sync.Queries;
using Collectibles.Application.Services;
using Collectibles.Application.Tests.Common;
using Collectibles.Domain.Entities;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Tests.Features.Sync;

/// <summary>Signature Moq needs to stub an out-parameter method.</summary>
public delegate bool TryDecodeCallback(string? hash, out long id);

public class SyncAuthorizationTests : BaseTestFixture
{
    private readonly Mock<IApplicationDbContextFactory> _contextFactoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    // Identifiers cross the sync HTTP boundary as HashIds, so the tests encode and decode
    // through a stand-in that round-trips rather than passing raw keys.
    private readonly Mock<IHashIdsService> _hashIdsServiceMock;

    public SyncAuthorizationTests()
    {
        _contextFactoryMock = new Mock<IApplicationDbContextFactory>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _hashIdsServiceMock = new Mock<IHashIdsService>();
        _hashIdsServiceMock
            .Setup(x => x.Encode(It.IsAny<long>()))
            .Returns((long id) => $"h{id}");
        _hashIdsServiceMock
            .Setup(x => x.TryDecode(It.IsAny<string>(), out It.Ref<long>.IsAny))
            .Returns(new TryDecodeCallback((string? hash, out long id) =>
            {
                id = 0;
                return hash is not null
                    && hash.StartsWith('h')
                    && long.TryParse(hash.AsSpan(1), out id);
            }));
        _currentUserServiceMock.Setup(x => x.UserId).Returns("test-user-id");
        _currentUserServiceMock.Setup(x => x.IsAdministrator).Returns(false);

        _contextFactoryMock
            .Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new NonDisposableDbContextWrapper(Context as IApplicationDbContext));
    }

    [Fact]
    public async Task HandleManifestForOwnedShowcaseShouldReturnAttachments()
    {
        var showcase = new Showcase
        {
            Name = "Owned Showcase",
            UserId = "test-user-id",
        };
        Context.Showcases.Add(showcase);

        var item = new CollectibleItem
        {
            Name = "Owned Item",
        };
        item.Showcases.Add(showcase);
        Context.CollectibleItems.Add(item);

        var attachment = new Attachment
        {
            Name = "owned",
            OriginalFilename = "owned.jpg",
            ContentHash = "owned-hash",
            FileSize = 42,
        };
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        Context.CollectibleItemAttachments.Add(new CollectibleItemAttachment
        {
            CollectibleItemId = item.Id,
            AttachmentId = attachment.Id,
        });
        await Context.SaveChangesAsync();

        var handler = new GetShowcaseManifestQueryHandler(
            _contextFactoryMock.Object,
            Mock.Of<IHashIdsService>(),
            Mock.Of<IEventLogService>(),
            _currentUserServiceMock.Object);

        var result = await handler.Handle(new GetShowcaseManifestQuery(showcase.Id), CancellationToken);

        result.Should().ContainSingle();
        result[0].OriginalFilename.Should().Be("owned.jpg");
        result[0].ContentHash.Should().Be("owned-hash");
    }

    [Fact]
    public async Task HandleManifestForOtherUsersShowcaseShouldThrowUnauthorizedAccessException()
    {
        var showcase = new Showcase
        {
            Name = "Other Showcase",
            UserId = "other-user-id",
        };
        Context.Showcases.Add(showcase);

        var item = new CollectibleItem
        {
            Name = "Other Item",
        };
        item.Showcases.Add(showcase);
        Context.CollectibleItems.Add(item);

        var attachment = new Attachment
        {
            Name = "other",
            OriginalFilename = "other.jpg",
            ContentHash = "other-hash",
            FileSize = 13,
        };
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        Context.CollectibleItemAttachments.Add(new CollectibleItemAttachment
        {
            CollectibleItemId = item.Id,
            AttachmentId = attachment.Id,
        });
        await Context.SaveChangesAsync();

        var handler = new GetShowcaseManifestQueryHandler(
            _contextFactoryMock.Object,
            Mock.Of<IHashIdsService>(),
            Mock.Of<IEventLogService>(),
            _currentUserServiceMock.Object);

        var act = async () => await handler.Handle(new GetShowcaseManifestQuery(showcase.Id), CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You are not authorized to access this showcase manifest.");
    }

    [Fact]
    public async Task HandleSyncUploadForOwnedShowcaseShouldInitiateUpload()
    {
        var showcase = new Showcase
        {
            Name = "Owned Showcase",
            UserId = "test-user-id",
        };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        var hierarchyServiceMock = new Mock<IItemHierarchyService>();
        hierarchyServiceMock
            .Setup(x => x.ResolveOrCreateHierarchyAsync(
                showcase.Id,
                It.Is<string[]>(segments => segments.SequenceEqual(new[] { "Folder" })),
                "test-user-id",
                It.IsAny<CancellationToken>(),
                It.IsAny<long?>()))
            .ReturnsAsync(123L);

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(
                It.Is<InitiateDirectUploadCommand>(command =>
                    command.ShowcaseId == showcase.Id &&
                    command.FileName == "photo.jpg" &&
                    command.ContentType == "image/jpeg" &&
                    command.FileSize == 512),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DirectUploadInitiation
            {
                UploadId = "upload-1",
                SasUrl = "https://example.test/upload",
                BlobName = "blob-1",
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            });

        var handler = new SyncUploadCommandHandler(
            _contextFactoryMock.Object,
            _currentUserServiceMock.Object,
            hierarchyServiceMock.Object,
            mediatorMock.Object,
            Mock.Of<ILogger<SyncUploadCommandHandler>>(),
            _hashIdsServiceMock.Object);

        var result = await handler.Handle(
            new SyncUploadCommand
            {
                ShowcaseId = showcase.Id,
                RelativePath = "Folder/photo.jpg",
                ContentHash = "hash-1",
                FileSize = 512,
                ContentType = "image/jpeg",
            }, CancellationToken);

        result.Skipped.Should().BeFalse();
        result.TargetItemHashId.Should().Be("h123");
        result.UploadId.Should().Be("upload-1");
        hierarchyServiceMock.VerifyAll();
        mediatorMock.VerifyAll();
    }

    [Fact]
    public async Task HandleSyncUploadForOtherUsersShowcaseShouldThrowUnauthorizedAccessException()
    {
        var showcase = new Showcase
        {
            Name = "Other Showcase",
            UserId = "other-user-id",
        };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        var hierarchyServiceMock = new Mock<IItemHierarchyService>();
        var mediatorMock = new Mock<IMediator>();

        var handler = new SyncUploadCommandHandler(
            _contextFactoryMock.Object,
            _currentUserServiceMock.Object,
            hierarchyServiceMock.Object,
            mediatorMock.Object,
            Mock.Of<ILogger<SyncUploadCommandHandler>>(),
            _hashIdsServiceMock.Object);

        var act = async () => await handler.Handle(
            new SyncUploadCommand
            {
                ShowcaseId = showcase.Id,
                RelativePath = "Folder/photo.jpg",
                ContentHash = "hash-1",
                FileSize = 512,
                ContentType = "image/jpeg",
            }, CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You are not authorized to upload files to this showcase.");

        hierarchyServiceMock.Verify(
            x => x.ResolveOrCreateHierarchyAsync(
                It.IsAny<long>(),
                It.IsAny<string[]>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<long?>()),
            Times.Never);
        mediatorMock.Verify(
            x => x.Send(It.IsAny<InitiateDirectUploadCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleCompleteSyncUploadForOwnedItemWithoutShowcaseIdShouldInferAuthorizedShowcase()
    {
        var showcase = new Showcase
        {
            Name = "Owned Showcase",
            UserId = "test-user-id",
        };
        Context.Showcases.Add(showcase);

        var item = new CollectibleItem
        {
            Name = "Owned Item",
        };
        item.Showcases.Add(showcase);
        Context.CollectibleItems.Add(item);
        await Context.SaveChangesAsync();

        var hierarchyServiceMock = new Mock<IItemHierarchyService>();
        hierarchyServiceMock
            .Setup(x => x.LinkAttachmentAsync(item.Id, 456L, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(
                It.Is<CompleteDirectUploadCommand>(command =>
                    command.ShowcaseId == showcase.Id &&
                    command.UploadId == "upload-1" &&
                    command.BlobName == "blob-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(456L);

        var handler = new CompleteSyncUploadCommandHandler(
            _contextFactoryMock.Object,
            _currentUserServiceMock.Object,
            hierarchyServiceMock.Object,
            mediatorMock.Object,
            Mock.Of<ILogger<CompleteSyncUploadCommandHandler>>(),
            _hashIdsServiceMock.Object);

        var result = await handler.Handle(
            new CompleteSyncUploadCommand
            {
                UploadId = "upload-1",
                BlobName = "blob-1",
                OriginalFileName = "photo.jpg",
                ContentType = "image/jpeg",
                FileSize = 1024,
                TargetItemHashId = $"h{item.Id}",
                ShowcaseId = null,
                ContentHash = "hash-1",
            }, CancellationToken);

        result.Should().Be(456L);
        hierarchyServiceMock.VerifyAll();
        mediatorMock.VerifyAll();
    }

    [Fact]
    public async Task HandleCompleteSyncUploadForOtherUsersItemShouldThrowUnauthorizedAccessException()
    {
        var showcase = new Showcase
        {
            Name = "Other Showcase",
            UserId = "other-user-id",
        };
        Context.Showcases.Add(showcase);

        var item = new CollectibleItem
        {
            Name = "Other Item",
        };
        item.Showcases.Add(showcase);
        Context.CollectibleItems.Add(item);
        await Context.SaveChangesAsync();

        var hierarchyServiceMock = new Mock<IItemHierarchyService>();
        var mediatorMock = new Mock<IMediator>();

        var handler = new CompleteSyncUploadCommandHandler(
            _contextFactoryMock.Object,
            _currentUserServiceMock.Object,
            hierarchyServiceMock.Object,
            mediatorMock.Object,
            Mock.Of<ILogger<CompleteSyncUploadCommandHandler>>(),
            _hashIdsServiceMock.Object);

        var act = async () => await handler.Handle(
            new CompleteSyncUploadCommand
            {
                UploadId = "upload-2",
                BlobName = "blob-2",
                OriginalFileName = "blocked.jpg",
                ContentType = "image/jpeg",
                FileSize = 2048,
                TargetItemHashId = $"h{item.Id}",
                ShowcaseId = null,
                ContentHash = "hash-2",
            }, CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You are not authorized to complete uploads for this item.");

        mediatorMock.Verify(
            x => x.Send(It.IsAny<CompleteDirectUploadCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
        hierarchyServiceMock.Verify(
            x => x.LinkAttachmentAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
