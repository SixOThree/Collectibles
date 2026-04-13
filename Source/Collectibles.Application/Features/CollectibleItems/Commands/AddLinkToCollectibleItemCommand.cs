using MediatR;

namespace Collectibles.Application.Features.CollectibleItems.Commands;

public class AddLinkToCollectibleItemCommand : IRequest<long>
{
    public long CollectibleItemId { get; set; }
    public string Url { get; set; } = string.Empty;
}
