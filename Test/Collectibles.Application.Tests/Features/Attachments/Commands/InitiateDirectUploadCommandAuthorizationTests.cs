using Collectibles.Application.Features.Attachments.Commands;
using Collectibles.Application.Tests.Common;
using Collectibles.Domain.Configuration.Storage;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Enums;
using Collectibles.Domain.Interfaces;

using Microsoft.Extensions.Options;

namespace Collectibles.Application.Tests.Features.Attachments.Commands;

public class InitiateDirectUploadCommandAuthorizationTests : BaseTestFixture
{
    private readonly Mock<IApplicationDbContextFactory> _contextFactoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IFileStorage> _fileStorageMock;
    private readonly IOptions<StorageSettings> _storageOptions;

    public InitiateDirectUploadCommandAuthorizationTests()
    {
        _contextFactoryMock = new Mock<IApplicationDbContextFactory>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _fileStorageMock = new Mock<IFileStorage>();
        _storageOptions = Options.Create(new StorageSettings
        {
            Provider = StorageProvider.AzureBlobStorage,
            DirectUpload = new DirectUploadSettings
            {
                Enabled = true,
                SasExpiryMinutes = 30,
            },
        });

        _currentUserServiceMock.Setup(x => x.UserId).Returns("test-user-id");
        _contextFactoryMock
            .Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new NonDisposableDbContextWrapper(Context as IApplicationDbContext));
    }

    [Fact]
    public async Task HandleOtherUsersShowcaseShouldThrowUnauthorizedAccessException()
    {
        _fileStorageMock.Setup(x => x.SupportsDirectUpload).Returns(true);

        var showcase = new Showcase
        {
            Name = "Other Showcase",
            UserId = "other-user-id",
        };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        var handler = new InitiateDirectUploadCommandHandler(
            _contextFactoryMock.Object,
            _fileStorageMock.Object,
            _storageOptions,
            Mock.Of<IEventLogService>(),
            _currentUserServiceMock.Object);

        var act = async () => await handler.Handle(
            new InitiateDirectUploadCommand
            {
                FileName = "blocked.jpg",
                FileSize = 100,
                ContentType = "image/jpeg",
                ShowcaseId = showcase.Id,
            }, CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You are not authorized to upload to this showcase.");

        _fileStorageMock.Verify(
            x => x.GenerateUploadSasUrl(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleOwnedShowcaseShouldReturnDirectUploadInitiation()
    {
        _fileStorageMock.Setup(x => x.SupportsDirectUpload).Returns(true);

        var showcase = new Showcase
        {
            Name = "Owned Showcase",
            UserId = "test-user-id",
        };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        _fileStorageMock
            .Setup(x => x.GenerateBlobName("photo.jpg", showcase.Id))
            .Returns("showcases/owned/photo.jpg");
        _fileStorageMock
            .Setup(x => x.GenerateUploadSasUrl("showcases/owned/photo.jpg", It.IsAny<TimeSpan>(), "image/jpeg"))
            .Returns("https://example.test/upload");

        var handler = new InitiateDirectUploadCommandHandler(
            _contextFactoryMock.Object,
            _fileStorageMock.Object,
            _storageOptions,
            Mock.Of<IEventLogService>(),
            _currentUserServiceMock.Object);

        var result = await handler.Handle(
            new InitiateDirectUploadCommand
            {
                FileName = "photo.jpg",
                FileSize = 200,
                ContentType = "image/jpeg",
                ShowcaseId = showcase.Id,
            }, CancellationToken);

        result.SasUrl.Should().Be("https://example.test/upload");
        result.BlobName.Should().Be("showcases/owned/photo.jpg");
        result.UploadId.Should().NotBeNullOrWhiteSpace();
    }
}
