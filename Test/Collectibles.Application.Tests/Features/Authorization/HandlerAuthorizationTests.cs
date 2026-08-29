using Collectibles.Application.Features.Attachments.Commands;
using Collectibles.Application.Features.CollectibleItems.Commands;
using Collectibles.Application.Features.ZipUpload.Commands;
using Collectibles.Application.Services;
using Collectibles.Application.Tests.Common;
using Collectibles.Domain.Configuration.Storage;
using Collectibles.Domain.Interfaces;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Collectibles.Application.Tests.Features.Authorization;

/// <summary>
/// Denial cases for handlers that previously enforced nothing while their siblings did.
/// The suite already covered the allow paths; these pin the deny paths so the checks
/// cannot quietly regress.
/// </summary>
public class HandlerAuthorizationTests : BaseTestFixture
{
    private const string CallerId = "test-user-id";
    private const string OtherUserId = "other-user-id";

    private readonly Mock<IApplicationDbContextFactory> _contextFactoryMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IEventLogService> _eventLogServiceMock = new();

    public HandlerAuthorizationTests()
    {
        _contextFactoryMock
            .Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new NonDisposableDbContextWrapper(Context));

        _currentUserServiceMock.Setup(x => x.UserId).Returns(CallerId);
        _currentUserServiceMock.Setup(x => x.IsAdministrator).Returns(false);
    }

    private async Task<Showcase> AddShowcaseAsync(string ownerId)
    {
        var showcase = new Showcase { Name = $"Showcase {Guid.NewGuid():N}", UserId = ownerId };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();
        return showcase;
    }

    [Fact]
    public async Task CreateCollectibleItemRejectsAShowcaseTheCallerDoesNotOwn()
    {
        var showcase = await AddShowcaseAsync(OtherUserId);

        var handler = new CreateCollectibleItemCommandHandler(
            _contextFactoryMock.Object,
            _eventLogServiceMock.Object,
            _currentUserServiceMock.Object);

        var act = async () => await handler.Handle(
            new CreateCollectibleItemCommand { ShowcaseId = showcase.Id, Name = "Item" },
            CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        (await Context.CollectibleItems.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateZipUploadJobRejectsAShowcaseTheCallerDoesNotOwn()
    {
        var showcase = await AddShowcaseAsync(OtherUserId);

        var handler = new CreateZipUploadJobCommandHandler(
            _contextFactoryMock.Object,
            _currentUserServiceMock.Object,
            Mock.Of<IFileStorage>(),
            Mock.Of<ILogger<CreateZipUploadJobCommandHandler>>(),
            _eventLogServiceMock.Object);

        var act = async () => await handler.Handle(
            new CreateZipUploadJobCommand
            {
                ShowcaseId = showcase.Id,
                FileName = "import.zip",
                FileSize = 10,
                Base64Content = Convert.ToBase64String("zip"u8.ToArray()),
            },
            CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task InitializeChunkedUploadRejectsAShowcaseTheCallerDoesNotOwn()
    {
        var showcase = await AddShowcaseAsync(OtherUserId);

        var handler = new InitializeChunkedUploadCommandHandler(
            _contextFactoryMock.Object,
            _currentUserServiceMock.Object,
            Mock.Of<ILogger<InitializeChunkedUploadCommandHandler>>(),
            _eventLogServiceMock.Object);

        var act = async () => await handler.Handle(
            new InitializeChunkedUploadCommand { ShowcaseId = showcase.Id, FileName = "import.zip", FileSize = 10 },
            CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        (await Context.ZipUploadJobs.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task UploadZipChunkRejectsAJobBelongingToAnotherUser()
    {
        var job = new ZipUploadJob
        {
            UserId = OtherUserId,
            ShowcaseId = 1,
            FileName = "import.zip",
            FileSize = 10,
        };
        Context.ZipUploadJobs.Add(job);
        await Context.SaveChangesAsync();

        var handler = new UploadZipChunkCommandHandler(
            _contextFactoryMock.Object,
            Mock.Of<IFileStorage>(),
            _currentUserServiceMock.Object,
            Mock.Of<ILogger<UploadZipChunkCommandHandler>>());

        var act = async () => await handler.Handle(
            new UploadZipChunkCommand
            {
                JobId = job.Id,
                ChunkIndex = 0,
                TotalChunks = 1,
                ChunkData = "chunk"u8.ToArray(),
                FileName = "import.zip",
                TotalFileSize = 5,
            },
            CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateAttachmentRejectsAShowcaseTheCallerDoesNotOwn()
    {
        var showcase = await AddShowcaseAsync(OtherUserId);

        var handler = new CreateAttachmentCommandHandler(
            _contextFactoryMock.Object,
            Mock.Of<IFileProcessingService>(),
            Mock.Of<IFileStorage>(),
            _eventLogServiceMock.Object,
            Mock.Of<IAttachmentHashService>(),
            Options.Create(new StorageSettings()),
            _currentUserServiceMock.Object);

        var act = async () => await handler.Handle(
            new CreateAttachmentCommand
            {
                Name = "file.pdf",
                ShowcaseId = showcase.Id,
                Base64Content = Convert.ToBase64String("content"u8.ToArray()),
            },
            CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task MoveAttachmentRejectsAnAttachmentTheCallerDoesNotOwn()
    {
        // The destination showcase belongs to the caller; the attachment does not. Checking
        // only the destination let a caller absorb any attachment into their own showcase.
        var destination = await AddShowcaseAsync(CallerId);
        var foreignShowcase = await AddShowcaseAsync(OtherUserId);

        var attachment = new Attachment { Name = "Foreign", CreatedBy = OtherUserId };
        var foreignItem = new CollectibleItem { Name = "Foreign item" };
        foreignItem.Showcases.Add(foreignShowcase);
        foreignItem.CollectibleItemAttachments.Add(new CollectibleItemAttachment { Attachment = attachment });
        Context.CollectibleItems.Add(foreignItem);
        await Context.SaveChangesAsync();

        var handler = new MoveAttachmentCommandHandler(
            _contextFactoryMock.Object,
            _eventLogServiceMock.Object,
            Mock.Of<IItemHierarchyService>(),
            _currentUserServiceMock.Object);

        var act = async () => await handler.Handle(
            new MoveAttachmentCommand
            {
                AttachmentId = attachment.Id,
                RelativePath = "Folder/stolen.jpg",
                ShowcaseId = destination.Id,
            },
            CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();

        var reloaded = await Context.Attachments.FirstAsync(a => a.Id == attachment.Id);
        reloaded.Name.Should().Be("Foreign");
    }

    [Fact]
    public async Task UnassignQRCodeRejectsAnItemTheCallerDoesNotOwn()
    {
        var showcase = await AddShowcaseAsync(OtherUserId);
        var item = new CollectibleItem { Name = "Foreign item" };
        item.Showcases.Add(showcase);
        Context.CollectibleItems.Add(item);
        Context.QRCodes.Add(new QRCode { Code = "QR-1", CollectibleItem = item, Status = QRCodeStatus.Assigned });
        await Context.SaveChangesAsync();

        var hashIdsMock = new Mock<IHashIdsService>();
        hashIdsMock.Setup(x => x.Decode(It.IsAny<string>())).Returns(item.Id);

        var handler = new Collectibles.Application.Features.QRCodes.Commands.UnassignQRCodeCommandHandler(
            Mock.Of<IQRCodeRepository>(),
            Context,
            hashIdsMock.Object,
            _currentUserServiceMock.Object);

        var act = async () => await handler.Handle(
            new Collectibles.Application.Features.QRCodes.Commands.UnassignQRCodeCommand { CollectibleItemHashId = "abc" },
            CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
