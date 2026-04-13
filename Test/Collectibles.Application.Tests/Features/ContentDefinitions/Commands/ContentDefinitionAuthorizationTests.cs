using Collectibles.Application.Features.ContentDefinitions.Commands;
using Collectibles.Application.Tests.Common;
using Collectibles.Domain.ValueObjects.Templates;

namespace Collectibles.Application.Tests.Features.ContentDefinitions.Commands;

public class ContentDefinitionAuthorizationTests : BaseTestFixture
{
    private readonly Mock<IApplicationDbContextFactory> _contextFactoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    public ContentDefinitionAuthorizationTests()
    {
        _contextFactoryMock = new Mock<IApplicationDbContextFactory>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns("test-user-id");

        _contextFactoryMock
            .Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new NonDisposableDbContextWrapper(Context as IApplicationDbContext));
    }

    [Fact]
    public async Task CreateForOtherUsersShowcaseShouldThrowUnauthorizedAccessException()
    {
        // Arrange: Create showcase owned by another user
        var showcase = new Showcase
        {
            Name = "Other User Showcase",
            UserId = "other-user-id",
        };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        var command = new CreateContentDefinitionCommand
        {
            IsGlobal = false,
            ShowcaseId = showcase.Id,
            Name = "Test",
            Fields = new List<FieldDefinitionDto>
            {
                new FieldDefinitionDto
                {
                    Name = "f1",
                    Label = "F1",
                    FieldType = FieldType.Text,
                    DisplayOrder = 1,
                },
            },
        };

        var handler = new CreateContentDefinitionCommandHandler(
            _contextFactoryMock.Object,
            Mock.Of<IEventLogService>(),
            _currentUserServiceMock.Object);

        // Act
        var act = async () => await handler.Handle(command, CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You are not authorized to create templates for this showcase.");
    }

    [Fact]
    public async Task DeleteOtherUsersTemplateShouldThrowUnauthorizedAccessException()
    {
        // Arrange: Create showcase owned by another user
        var showcase = new Showcase
        {
            Name = "Other User Showcase",
            UserId = "other-user-id",
        };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        // Create a content definition linked to that showcase, owned by another user
        var contentDefinition = new ContentDefinition
        {
            IsGlobal = false,
            ShowcaseId = showcase.Id,
            IsActive = true,
            CreatedBy = "other-user-id",
        };
        contentDefinition.SetTemplateDefinition(new TemplateDefinition
        {
            Name = "Other User Template",
            Fields = new List<FieldDefinition>
            {
                new FieldDefinition
                {
                    Name = "f1",
                    Label = "F1",
                    FieldType = FieldType.Text,
                    DisplayOrder = 1,
                },
            },
        });
        Context.ContentDefinitions.Add(contentDefinition);
        await Context.SaveChangesAsync();

        var command = new DeleteContentDefinitionCommand
        {
            Id = contentDefinition.Id,
        };

        var handler = new DeleteContentDefinitionCommandHandler(
            _contextFactoryMock.Object,
            Mock.Of<IEventLogService>(),
            _currentUserServiceMock.Object);

        // Act
        var act = async () => await handler.Handle(command, CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You are not authorized to delete this template.");
    }
}
