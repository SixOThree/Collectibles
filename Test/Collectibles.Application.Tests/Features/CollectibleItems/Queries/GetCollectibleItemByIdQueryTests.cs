using Collectibles.Application.Features.CollectibleItems.Queries;
using Collectibles.Application.Mappings.Explicit;
using Collectibles.Application.Services;
using Collectibles.Application.Tests.Common;
using Collectibles.Domain.ValueObjects.Templates;

using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Tests.Features.CollectibleItems.Queries;

public class GetCollectibleItemByIdQueryTests : BaseTestFixture
{
    [Fact]
    public async Task Handle_ReturnsTemplateItemDetailPreviewHeightOverride()
    {
        var contentDefinition = new ContentDefinition
        {
            IsActive = true,
            IsGlobal = true,
            ItemDetailPreviewHeight = 320,
        };

        contentDefinition.SetTemplateDefinition(new TemplateDefinition
        {
            Name = "Posters",
            Fields =
            [
                new FieldDefinition
                {
                    Name = "title",
                    Label = "Title",
                    FieldType = FieldType.Text,
                    DisplayOrder = 0,
                },
            ],
        });

        Context.ContentDefinitions.Add(contentDefinition);
        await Context.SaveChangesAsync();

        var collectibleItem = new CollectibleItem
        {
            Name = "Metroid Prime Poster",
            ContentDefinitionId = contentDefinition.Id,
            ContentType = contentDefinition,
        };

        Context.CollectibleItems.Add(collectibleItem);
        await Context.SaveChangesAsync();

        var hashIdsServiceMock = new Mock<IHashIdsService>();
        hashIdsServiceMock
            .Setup(x => x.Encode(It.IsAny<long>()))
            .Returns<long>(id => $"hash-{id}");

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.UserId).Returns("test-user-id");

        var handler = new GetCollectibleItemByIdQueryHandler(
            Context,
            new CollectibleItemMappingService(
                hashIdsServiceMock.Object,
                Mock.Of<ILogger<CollectibleItemMappingService>>()),
            Mock.Of<IEventLogService>(),
            currentUserServiceMock.Object);

        var result = await handler.Handle(
            new GetCollectibleItemByIdQuery { Id = collectibleItem.Id },
            CancellationToken);

        result.Should().NotBeNull();
        result!.ItemDetailPreviewHeight.Should().Be(320);
    }
}
