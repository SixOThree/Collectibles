using Collectibles.Application.Features.QRCodes.Commands;
using Collectibles.Application.Services;
using Collectibles.Application.Tests.Common;
using Collectibles.Domain.Interfaces;

namespace Collectibles.Application.Tests.Features.QRCodes.Commands;

public class AssignQRCodeAuthorizationTests : BaseTestFixture
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IHashIdsService> _hashIdsMock;
    private readonly Mock<IQRCodeRepository> _qrCodeRepoMock;

    public AssignQRCodeAuthorizationTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns("test-user-id");

        _hashIdsMock = new Mock<IHashIdsService>();
        _qrCodeRepoMock = new Mock<IQRCodeRepository>();
    }

    [Fact]
    public async Task HandleOtherUsersItemShouldReturnNotAuthorized()
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
        var item = new CollectibleItem
        {
            Name = "Other User Item",
        };
        item.Showcases.Add(showcase);
        Context.CollectibleItems.Add(item);
        await Context.SaveChangesAsync();

        // Mock IHashIdsService to return the item's ID for any input
        _hashIdsMock.Setup(x => x.Decode(It.IsAny<string>())).Returns(item.Id);

        // Mock IQRCodeRepository: GetByCodeAsync returns null (new QR code), AddAsync completes
        _qrCodeRepoMock.Setup(x => x.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QRCode?)null);
        _qrCodeRepoMock.Setup(x => x.AddAsync(It.IsAny<QRCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QRCode qr, CancellationToken _) => qr);

        var handler = new AssignQRCodeCommandHandler(
            _qrCodeRepoMock.Object,
            Context,
            _hashIdsMock.Object,
            _currentUserServiceMock.Object);

        var command = new AssignQRCodeCommand
        {
            QRCode = "TEST123",
            CollectibleItemHashId = "hash",
        };

        var result = await handler.Handle(command, CancellationToken);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not authorized");
    }
}
