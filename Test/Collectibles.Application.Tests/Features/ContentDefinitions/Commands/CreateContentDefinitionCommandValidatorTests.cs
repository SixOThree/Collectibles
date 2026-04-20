using Collectibles.Application.Features.ContentDefinitions.Commands;
using Collectibles.Application.Tests.Helpers;
using Collectibles.Domain.ValueObjects.Templates;

namespace Collectibles.Application.Tests.Features.ContentDefinitions.Commands;

public class CreateContentDefinitionCommandValidatorTests
{
    private readonly CreateContentDefinitionCommandValidator _validator = new();

    [Fact]
    public void ShouldNotHaveValidationErrorWhenItemDetailPreviewHeightIsNull()
    {
        var command = CreateValidCommand();
        command.ItemDetailPreviewHeight = null;

        ValidationTestHelper.ShouldNotHaveValidationErrorFor(
            _validator,
            command,
            x => x.ItemDetailPreviewHeight);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(260)]
    [InlineData(500)]
    public void ShouldNotHaveValidationErrorWhenItemDetailPreviewHeightIsWithinRange(int height)
    {
        var command = CreateValidCommand();
        command.ItemDetailPreviewHeight = height;

        ValidationTestHelper.ShouldNotHaveValidationErrorFor(
            _validator,
            command,
            x => x.ItemDetailPreviewHeight);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(501)]
    public void ShouldHaveValidationErrorWhenItemDetailPreviewHeightIsOutsideRange(int height)
    {
        var command = CreateValidCommand();
        command.ItemDetailPreviewHeight = height;

        ValidationTestHelper.ShouldHaveValidationErrorFor(
            _validator,
            command,
            x => x.ItemDetailPreviewHeight,
            "Item page preview height override must be between 100 and 500 pixels when provided.");
    }

    private static CreateContentDefinitionCommand CreateValidCommand()
    {
        return new CreateContentDefinitionCommand
        {
            Name = "Trading Cards",
            IsGlobal = true,
            Fields =
            [
                new FieldDefinitionDto
                {
                    Name = "series",
                    Label = "Series",
                    FieldType = FieldType.Text,
                    DisplayOrder = 0,
                },
            ],
        };
    }
}
