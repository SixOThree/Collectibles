using Collectibles.Application.Features.Attachments.Commands;
using Collectibles.Application.Tests.Common;
using Collectibles.Domain.Entities;

namespace Collectibles.Application.Tests.Features.Attachments.Commands;

public class UpdateAttachmentFileSizesCommandAuthorizationTests : BaseTestFixture
{
    private readonly Mock<IApplicationDbContextFactory> _contextFactoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    public UpdateAttachmentFileSizesCommandAuthorizationTests()
    {
        _contextFactoryMock = new Mock<IApplicationDbContextFactory>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns("test-user-id");
        _currentUserServiceMock.Setup(x => x.IsAdministrator).Returns(false);

        _contextFactoryMock
            .Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new NonDisposableDbContextWrapper(Context as IApplicationDbContext));
    }

    [Fact]
    public async Task HandleNonAdminShouldThrowUnauthorizedAccessException()
    {
        var attachment = new Attachment
        {
            Name = "Needs Size",
            FileSize = 0,
            AttachmentContent = new AttachmentContent
            {
                Content = new byte[] { 1, 2, 3, 4 },
            },
        };
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        var handler = new UpdateAttachmentFileSizesCommandHandler(
            _contextFactoryMock.Object,
            _currentUserServiceMock.Object);

        var act = async () => await handler.Handle(new UpdateAttachmentFileSizesCommand(), CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You are not authorized to update attachment file sizes.");
    }

    [Fact]
    public async Task HandleAdminShouldUpdateZeroByteAttachments()
    {
        _currentUserServiceMock.Setup(x => x.IsAdministrator).Returns(true);

        var attachment = new Attachment
        {
            Name = "Needs Size",
            FileSize = 0,
            AttachmentContent = new AttachmentContent
            {
                Content = new byte[] { 1, 2, 3, 4, 5 },
            },
        };
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        var handler = new UpdateAttachmentFileSizesCommandHandler(
            _contextFactoryMock.Object,
            _currentUserServiceMock.Object);

        var updatedCount = await handler.Handle(new UpdateAttachmentFileSizesCommand(), CancellationToken);

        updatedCount.Should().Be(1);
        attachment.FileSize.Should().Be(5);
    }
}
