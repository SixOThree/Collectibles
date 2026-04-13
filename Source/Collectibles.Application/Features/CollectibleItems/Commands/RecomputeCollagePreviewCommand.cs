using MediatR;

namespace Collectibles.Application.Features.CollectibleItems.Commands;

public class RecomputeCollagePreviewCommand : IRequest<RecomputeCollagePreviewResult>
{
    public long CollectibleItemId { get; set; }

    public RecomputeCollagePreviewCommand(long collectibleItemId)
    {
        CollectibleItemId = collectibleItemId;
    }
}

public class RecomputeCollagePreviewResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Base64Thumbnail { get; set; }
}
