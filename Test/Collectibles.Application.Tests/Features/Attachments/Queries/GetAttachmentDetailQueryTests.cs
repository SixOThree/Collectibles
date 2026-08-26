using Collectibles.Application.Common.Authorization.Requirements;
using Collectibles.Application.Features.Attachments;
using Collectibles.Application.Features.Attachments.Queries;
using Collectibles.Application.Mappings.Explicit;
using Collectibles.Application.Tests.Common;
using Microsoft.AspNetCore.Authorization;
using Moq;

namespace Collectibles.Application.Tests.Features.Attachments.Queries;

public class GetAttachmentDetailQueryTests : QueryTestBase<GetAttachmentDetailQuery, AttachmentDto>
{
    private readonly Mock<IAttachmentMappingService> _attachmentMappingServiceMock = new();
    private readonly Mock<IAuthorizationService> _authorizationServiceMock = new();
    private readonly Mock<IEventLogService> _eventLogServiceMock = new();

    public GetAttachmentDetailQueryTests()
    {
        _attachmentMappingServiceMock.Setup(x => x.MapWithContentAsync(
                It.IsAny<Domain.Entities.Attachment>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Attachment a, CancellationToken ct) =>
                new AttachmentDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Created = a.Created ?? DateTime.MinValue,
                    CreatedBy = a.CreatedBy,
                });

        _authorizationServiceMock.Setup(x => x.AuthorizeAsync(
                It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                It.IsAny<object>(),
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());
    }

    protected override IRequestHandler<GetAttachmentDetailQuery, AttachmentDto> CreateHandler()
    {
        return new GetAttachmentDetailQueryHandler(
            Context,
            _attachmentMappingServiceMock.Object,
            _eventLogServiceMock.Object,
            _authorizationServiceMock.Object);
    }

    [Fact]
    public async Task HandleAuthorizedAttachmentShouldReturnAttachmentDto()
    {
        var attachment = new Domain.Entities.Attachment { Name = "Test Attachment" };
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        var result = await Act(new GetAttachmentDetailQuery { Id = attachment.Id });

        result.Should().NotBeNull();
        result.Id.Should().Be(attachment.Id);
        _eventLogServiceMock.Verify(
            x => x.LogEventAsync(
                EventAction.View,
                nameof(Domain.Entities.Attachment),
                attachment.Id,
                attachment.Name,
                It.IsAny<object?>(),
                It.IsAny<object?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleUnauthorizedAttachmentShouldThrow()
    {
        var attachment = new Domain.Entities.Attachment { Name = "Private Attachment" };
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        _authorizationServiceMock
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                It.IsAny<object>(),
                It.Is<IEnumerable<IAuthorizationRequirement>>(requirements =>
                    requirements.OfType<ViewAttachmentRequirement>().Any())))
            .ReturnsAsync(AuthorizationResult.Failed());

        var act = async () => await Act(new GetAttachmentDetailQuery { Id = attachment.Id });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _attachmentMappingServiceMock.Verify(
            x => x.MapWithContentAsync(It.IsAny<Domain.Entities.Attachment>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _eventLogServiceMock.Verify(
            x => x.LogEventAsync(
                It.IsAny<EventAction>(),
                It.IsAny<string?>(),
                It.IsAny<long?>(),
                It.IsAny<string?>(),
                It.IsAny<object?>(),
                It.IsAny<object?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleNonexistentAttachmentShouldReturnNull()
    {
        var result = await Act(new GetAttachmentDetailQuery { Id = 999L });

        result.Should().BeNull();
        _authorizationServiceMock.Verify(
            x => x.AuthorizeAsync(
                It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                It.IsAny<object>(),
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()),
            Times.Never);
    }
}
