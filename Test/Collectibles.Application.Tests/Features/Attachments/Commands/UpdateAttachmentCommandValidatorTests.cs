using Collectibles.Application.Features.Attachments.Commands;
using Collectibles.Application.Tests.Helpers;

namespace Collectibles.Application.Tests.Features.Attachments.Commands;

public class UpdateAttachmentCommandValidatorTests
{
    private readonly UpdateAttachmentCommandValidator _validator = new();

    [Fact]
    public async Task ShouldBeValidWhenAllPropertiesAreValid()
    {
        // Arrange
        var command = new UpdateAttachmentCommand
        {
            Id = 1,
            Name = "Updated Attachment",
            OriginalFilename = "updated.jpg",
            FileType = "image/jpeg",
            AttachmentType = AttachmentType.Image,
            Base64Content = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }),
            Base64PreviewThumbnail = Convert.ToBase64String(new byte[] { 5, 6, 7, 8 }),
        };

        // Act
        var result = await ValidationTestHelper.ValidateAsync(_validator, command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldBeValidWhenOptionalPropertiesAreNull()
    {
        // Arrange
        var command = new UpdateAttachmentCommand
        {
            Id = 1,
            Name = "Updated Attachment",
            OriginalFilename = null,
            FileType = null,
            AttachmentType = null,
            Base64Content = null,
            Base64PreviewThumbnail = null,
        };

        // Act
        var result = await ValidationTestHelper.ValidateAsync(_validator, command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ShouldHaveValidationErrorWhenNameIsEmpty(string? name)
    {
        // Arrange
        var command = new UpdateAttachmentCommand
        {
            Id = 1,
            Name = name!,
        };

        // Act & Assert
        ValidationTestHelper.ShouldHaveValidationErrorFor(
            _validator,
            command,
            x => x.Name);
    }

    [Fact]
    public void ShouldHaveValidationErrorWhenNameExceedsMaxLength()
    {
        // Arrange
        var command = new UpdateAttachmentCommand
        {
            Id = 1,
            Name = new string('A', 256), // 256 characters, exceeds 255 max
        };

        // Act & Assert
        ValidationTestHelper.ShouldHaveValidationErrorFor(
            _validator,
            command,
            x => x.Name);
    }

    [Fact]
    public void ShouldNotHaveValidationErrorWhenNameIsMaxLength()
    {
        // Arrange
        var command = new UpdateAttachmentCommand
        {
            Id = 1,
            Name = new string('A', 255), // Exactly 255 characters
        };

        // Act & Assert
        ValidationTestHelper.ShouldNotHaveValidationErrorFor(
            _validator,
            command,
            x => x.Name);
    }

    [Fact]
    public void ShouldHaveValidationErrorWhenOriginalFilenameExceedsMaxLength()
    {
        // Arrange
        var command = new UpdateAttachmentCommand
        {
            Id = 1,
            Name = "Valid Name",
            OriginalFilename = new string('A', 256), // 256 characters, exceeds 255 max
        };

        // Act & Assert
        ValidationTestHelper.ShouldHaveValidationErrorFor(
            _validator,
            command,
            x => x.OriginalFilename);
    }

    [Fact]
    public void ShouldNotHaveValidationErrorWhenOriginalFilenameIsMaxLength()
    {
        // Arrange
        var command = new UpdateAttachmentCommand
        {
            Id = 1,
            Name = "Valid Name",
            OriginalFilename = new string('A', 255), // Exactly 255 characters
        };

        // Act & Assert
        ValidationTestHelper.ShouldNotHaveValidationErrorFor(
            _validator,
            command,
            x => x.OriginalFilename);
    }

    [Fact]
    public void ShouldHaveValidationErrorWhenFileTypeExceedsMaxLength()
    {
        // Arrange
        var command = new UpdateAttachmentCommand
        {
            Id = 1,
            Name = "Valid Name",
            FileType = new string('A', 101), // 101 characters, exceeds 100 max
        };

        // Act & Assert
        ValidationTestHelper.ShouldHaveValidationErrorFor(
            _validator,
            command,
            x => x.FileType);
    }

    [Fact]
    public void ShouldNotHaveValidationErrorWhenFileTypeIsMaxLength()
    {
        // Arrange
        var command = new UpdateAttachmentCommand
        {
            Id = 1,
            Name = "Valid Name",
            FileType = new string('A', 100), // Exactly 100 characters
        };

        // Act & Assert
        ValidationTestHelper.ShouldNotHaveValidationErrorFor(
            _validator,
            command,
            x => x.FileType);
    }

    [Fact]
    public void ShouldHaveValidationErrorWhenBase64ContentIsInvalid()
    {
        // Arrange
        var command = new UpdateAttachmentCommand
        {
            Id = 1,
            Name = "Valid Name",
            Base64Content = "InvalidBase64Content!",
        };

        // Act & Assert
        ValidationTestHelper.ShouldHaveValidationErrorFor(
            _validator,
            command,
            x => x.Base64Content,
            "Content must be valid base64 encoded string.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ShouldNotHaveValidationErrorWhenBase64ContentIsNullOrEmpty(string? base64Content)
    {
        // Arrange
        var command = new UpdateAttachmentCommand
        {
            Id = 1,
            Name = "Valid Name",
            Base64Content = base64Content,
        };

        // Act & Assert
        ValidationTestHelper.ShouldNotHaveValidationErrorFor(
            _validator,
            command,
            x => x.Base64Content);
    }

    [Fact]
    public void ShouldNotHaveValidationErrorWhenBase64ContentIsValid()
    {
        // Arrange
        var command = new UpdateAttachmentCommand
        {
            Id = 1,
            Name = "Valid Name",
            Base64Content = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }),
        };

        // Act & Assert
        ValidationTestHelper.ShouldNotHaveValidationErrorFor(
            _validator,
            command,
            x => x.Base64Content);
    }

    [Fact]
    public void ShouldHaveValidationErrorWhenBase64PreviewThumbnailIsInvalid()
    {
        // Arrange
        var command = new UpdateAttachmentCommand
        {
            Id = 1,
            Name = "Valid Name",
            Base64PreviewThumbnail = "InvalidBase64Content!",
        };

        // Act & Assert
        ValidationTestHelper.ShouldHaveValidationErrorFor(
            _validator,
            command,
            x => x.Base64PreviewThumbnail,
            "Preview thumbnail must be valid base64 encoded string.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ShouldNotHaveValidationErrorWhenBase64PreviewThumbnailIsNullOrEmpty(string? base64PreviewThumbnail)
    {
        // Arrange
        var command = new UpdateAttachmentCommand
        {
            Id = 1,
            Name = "Valid Name",
            Base64PreviewThumbnail = base64PreviewThumbnail,
        };

        // Act & Assert
        ValidationTestHelper.ShouldNotHaveValidationErrorFor(
            _validator,
            command,
            x => x.Base64PreviewThumbnail);
    }

    [Fact]
    public void ShouldNotHaveValidationErrorWhenBase64PreviewThumbnailIsValid()
    {
        // Arrange
        var command = new UpdateAttachmentCommand
        {
            Id = 1,
            Name = "Valid Name",
            Base64PreviewThumbnail = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }),
        };

        // Act & Assert
        ValidationTestHelper.ShouldNotHaveValidationErrorFor(
            _validator,
            command,
            x => x.Base64PreviewThumbnail);
    }
}
