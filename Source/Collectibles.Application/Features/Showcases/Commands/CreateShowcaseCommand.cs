using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using MediatR;

namespace Collectibles.Application.Features.Showcases.Commands;

public record CreateShowcaseCommand : IRequest<long>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public long? PreviewImageId { get; init; }
    public bool IsPrivate { get; init; } = true;
    public List<long> TagIds { get; init; } = new();
    public string? UserId { get; init; } // Optional UserId to handle Blazor context issues
}

public class CreateShowcaseCommandHandler : IRequestHandler<CreateShowcaseCommand, long>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEventLogService _eventLogService;

    public CreateShowcaseCommandHandler(
        IApplicationDbContextFactory contextFactory,
        ICurrentUserService currentUserService,
        IEventLogService eventLogService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _eventLogService = eventLogService;
    }

    public async Task<long> Handle(CreateShowcaseCommand request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Use the provided UserId if available, otherwise fall back to CurrentUserService
        var userId = request.UserId ?? _currentUserService.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User context not available. Please ensure you are logged in.");
        }

        var showcase = new Showcase
        {
            Name = request.Name,
            Description = request.Description,
            UserId = userId,
            IsPrivate = request.IsPrivate,

            // Explicitly set audit fields when UserId is provided
            CreatedBy = userId,
            Created = DateTime.UtcNow,
        };

        if (request.PreviewImageId.HasValue)
        {
            var previewImage = await context.Attachments
                .FindAsync(new object[] { request.PreviewImageId.Value }, cancellationToken);

            if (previewImage != null)
            {
                showcase.PreviewImage = previewImage;
            }
        }

        if (request.TagIds.Count != 0)
        {
            showcase.ShowcaseTags = request.TagIds
                .Select(tagId => new ShowcaseTag
                {
                    TagId = tagId,
                    Showcase = showcase,
                })
                .ToList();
        }

        context.Showcases.Add(showcase);
        await context.SaveChangesAsync(cancellationToken);

        // Log the creation event
        await _eventLogService.LogEventAsync(
            EventAction.Create,
            nameof(Showcase),
            showcase.Id,
            showcase.Name,
            null,
            new
            {
                Name = request.Name,
                Description = request.Description,
                IsPrivate = request.IsPrivate,
                PreviewImageId = request.PreviewImageId,
                TagIds = request.TagIds,
                UserId = userId,
            },
            cancellationToken: cancellationToken);

        return showcase.Id;
    }
}
