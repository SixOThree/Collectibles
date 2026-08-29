using Collectibles.Application.Features.Attachments;
using Collectibles.Application.Features.Attachments.Queries;
using Collectibles.Application.Mappings.Explicit;
using Collectibles.Application.Tests.Common;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Tests.Features.Attachments.Queries;

public class GetAttachmentForPreviewQueryTests : IDisposable
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly Mock<IAttachmentMappingService> _attachmentMappingServiceMock;
    private readonly Mock<IAuthorizationService> _authorizationServiceMock;
    private readonly Mock<IEventLogService> _eventLogServiceMock;
    private readonly Mock<ILogger<GetAttachmentForPreviewQueryHandler>> _loggerMock;
    private readonly ApplicationDbContext _context;

    public GetAttachmentForPreviewQueryTests()
    {
        _attachmentMappingServiceMock = new Mock<IAttachmentMappingService>();
        _authorizationServiceMock = new Mock<IAuthorizationService>();
        _authorizationServiceMock
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());
        _eventLogServiceMock = new Mock<IEventLogService>();
        _loggerMock = new Mock<ILogger<GetAttachmentForPreviewQueryHandler>>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var currentUserService = Mock.Of<ICurrentUserService>();
        _context = new ApplicationDbContext(options, currentUserService);
        _context.Database.EnsureCreated();

        var contextFactoryMock = new Mock<IApplicationDbContextFactory>();

        // The handler owns the context it is given (await using), so hand it a wrapper the
        // test can keep using across the multiple Handle calls these tests make.
        contextFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new NonDisposableDbContextWrapper(_context));
        _contextFactory = contextFactoryMock.Object;
    }

    [Fact]
    public async Task HandleAttachmentWithExternalStorageShouldReturnContentFromFileStorage()
    {
        // Arrange
        var externalContent = "External file content"u8.ToArray();
        var externalPreview = "External preview content"u8.ToArray();

        var attachment = new Attachment
        {
            Name = "Test External Attachment",
            FilePath = "/storage/attachments/test-file.pdf",
            PreviewPath = "/storage/previews/test-preview.jpg",
            FileType = "application/pdf",
            AttachmentType = AttachmentType.Document,
        };

        await _context.Attachments.AddAsync(attachment);
        await _context.SaveChangesAsync();

        _attachmentMappingServiceMock.Setup(x => x.MapWithContentAsync(
                It.IsAny<Attachment>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AttachmentDto
            {
                Id = attachment.Id,
                Name = attachment.Name,
                FileType = attachment.FileType,
                AttachmentType = attachment.AttachmentType,
                Base64Content = Convert.ToBase64String(externalContent),
                Base64PreviewThumbnail = Convert.ToBase64String(externalPreview),
            });

        var handler = new GetAttachmentForPreviewQueryHandler(_contextFactory, _attachmentMappingServiceMock.Object, _eventLogServiceMock.Object, _authorizationServiceMock.Object, _loggerMock.Object);
        var query = new GetAttachmentForPreviewQuery(attachment.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Base64Content.Should().Be(Convert.ToBase64String(externalContent));
        result.Base64PreviewThumbnail.Should().Be(Convert.ToBase64String(externalPreview));
        _attachmentMappingServiceMock.Verify(
            x => x.MapWithContentAsync(
            It.IsAny<Attachment>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAttachmentWithBothStorageMethodsShouldPreferExternal()
    {
        // Arrange
        var databaseContent = "Database content"u8.ToArray();
        var externalContent = "External content"u8.ToArray();

        var attachment = new Attachment
        {
            Name = "Test Mixed Storage",
            FilePath = "/storage/attachments/test-file.pdf",
            FileType = "application/pdf",
            AttachmentType = AttachmentType.Document,
            AttachmentContent = new AttachmentContent
            {
                Content = databaseContent,
            },
        };

        await _context.Attachments.AddAsync(attachment);
        await _context.SaveChangesAsync();

        _attachmentMappingServiceMock.Setup(x => x.MapWithContentAsync(
                It.IsAny<Attachment>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AttachmentDto
            {
                Id = attachment.Id,
                Name = attachment.Name,
                FileType = attachment.FileType,
                AttachmentType = attachment.AttachmentType,
                Base64Content = Convert.ToBase64String(externalContent),
            });

        var handler = new GetAttachmentForPreviewQueryHandler(_contextFactory, _attachmentMappingServiceMock.Object, _eventLogServiceMock.Object, _authorizationServiceMock.Object, _loggerMock.Object);
        var query = new GetAttachmentForPreviewQuery(attachment.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Base64Content.Should().Be(Convert.ToBase64String(externalContent));
        _attachmentMappingServiceMock.Verify(
            x => x.MapWithContentAsync(
            It.IsAny<Attachment>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAttachmentWithDatabaseStorageOnlyShouldReturnDatabaseContent()
    {
        // Arrange
        var databaseContent = "Database content"u8.ToArray();

        var attachment = new Attachment
        {
            Name = "Test Database Storage",
            FileType = "application/pdf",
            AttachmentType = AttachmentType.Document,
            AttachmentContent = new AttachmentContent
            {
                Content = databaseContent,
            },
        };

        await _context.Attachments.AddAsync(attachment);
        await _context.SaveChangesAsync();

        _attachmentMappingServiceMock.Setup(x => x.MapWithContentAsync(
                It.IsAny<Attachment>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Attachment a, CancellationToken ct) =>
                new AttachmentDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    FileType = a.FileType,
                    AttachmentType = a.AttachmentType,
                    Base64Content = a.AttachmentContent?.Content != null
                        ? Convert.ToBase64String(a.AttachmentContent.Content) : null,
                });

        var handler = new GetAttachmentForPreviewQueryHandler(_contextFactory, _attachmentMappingServiceMock.Object, _eventLogServiceMock.Object, _authorizationServiceMock.Object, _loggerMock.Object);
        var query = new GetAttachmentForPreviewQuery(attachment.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Base64Content.Should().Be(Convert.ToBase64String(databaseContent));
        _attachmentMappingServiceMock.Verify(
            x => x.MapWithContentAsync(
            It.IsAny<Attachment>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleEveryRequestShouldReAuthorizeAndReReadRatherThanServeACachedCopy()
    {
        // Arrange
        var attachment = new Attachment
        {
            Name = "Test Attachment",
            FileType = "application/pdf",
            AttachmentType = AttachmentType.Document,
        };

        await _context.Attachments.AddAsync(attachment);
        await _context.SaveChangesAsync();

        _attachmentMappingServiceMock.Setup(x => x.MapWithContentAsync(
                It.IsAny<Attachment>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Attachment a, CancellationToken ct) =>
                new AttachmentDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    FileType = a.FileType,
                    AttachmentType = a.AttachmentType,
                });

        var handler = new GetAttachmentForPreviewQueryHandler(_contextFactory, _attachmentMappingServiceMock.Object, _eventLogServiceMock.Object, _authorizationServiceMock.Object, _loggerMock.Object);
        var query = new GetAttachmentForPreviewQuery(attachment.Id);

        // Act
        await handler.Handle(query, CancellationToken.None);
        await handler.Handle(query, CancellationToken.None);

        // Assert: the previous id-keyed memory cache served one user's content to another
        // and stayed stale after edits, so each request must re-read and re-authorize.
        var contextFactoryMock = Mock.Get(_contextFactory);
        contextFactoryMock.Verify(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _authorizationServiceMock.Verify(
            x => x.AuthorizeAsync(
                It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task HandleUnauthorizedCallerShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var attachment = new Attachment
        {
            Name = "Private Attachment",
            FileType = "application/pdf",
            AttachmentType = AttachmentType.Document,
        };

        await _context.Attachments.AddAsync(attachment);
        await _context.SaveChangesAsync();

        _authorizationServiceMock
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Failed());

        var handler = new GetAttachmentForPreviewQueryHandler(_contextFactory, _attachmentMappingServiceMock.Object, _eventLogServiceMock.Object, _authorizationServiceMock.Object, _loggerMock.Object);

        // Act
        var act = async () => await handler.Handle(new GetAttachmentForPreviewQuery(attachment.Id), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _attachmentMappingServiceMock.Verify(
            x => x.MapWithContentAsync(It.IsAny<Attachment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleNonExistentAttachmentShouldThrowArgumentException()
    {
        // Arrange
        var handler = new GetAttachmentForPreviewQueryHandler(_contextFactory, _attachmentMappingServiceMock.Object, _eventLogServiceMock.Object, _authorizationServiceMock.Object, _loggerMock.Object);
        var query = new GetAttachmentForPreviewQuery(999L);

        // Act
        var act = async () => await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Attachment with ID 999 not found. (Parameter 'request')");
    }

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}
