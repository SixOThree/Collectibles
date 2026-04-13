using MediatR;

namespace Collectibles.Application.Features.CollectibleItems.Commands;

public class RefreshLinkCacheCommand : IRequest
{
    public long LinkInfoId { get; set; }
}
