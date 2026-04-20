using Collectibles.Application.Features.ContentDefinitions.Queries;
using Collectibles.Application.Interfaces;
using Collectibles.Application.Tests.Common;
using Collectibles.Domain.ValueObjects.Templates;

namespace Collectibles.Application.Tests.Features.ContentDefinitions.Queries;

public class GetContentDefinitionByIdQueryTests : BaseTestFixture
{
    private readonly Mock<IApplicationDbContextFactory> _contextFactoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    public GetContentDefinitionByIdQueryTests()
    {
        _contextFactoryMock = new Mock<IApplicationDbContextFactory>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns("test-user-id");

        _contextFactoryMock
            .Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new NonDisposableDbContextWrapper(Context as IApplicationDbContext));
    }

    [Fact]
    public async Task Handle_ReturnsItemDetailPreviewHeightOverride()
    {
        var contentDefinition = new ContentDefinition
        {
            IsActive = true,
            IsGlobal = true,
            ItemDetailPreviewHeight = 260,
        };

        contentDefinition.SetTemplateDefinition(new TemplateDefinition
        {
            Name = "Trading Cards",
            Fields =
            [
                new FieldDefinition
                {
                    Name = "series",
                    Label = "Series",
                    FieldType = FieldType.Text,
                    DisplayOrder = 0,
                },
            ],
        });

        Context.ContentDefinitions.Add(contentDefinition);
        await Context.SaveChangesAsync();

        var handler = new GetContentDefinitionByIdQueryHandler(
            _contextFactoryMock.Object,
            _currentUserServiceMock.Object);

        var result = await handler.Handle(
            new GetContentDefinitionByIdQuery(contentDefinition.Id),
            CancellationToken);

        result.Should().NotBeNull();
        result!.ItemDetailPreviewHeight.Should().Be(260);
    }
}
