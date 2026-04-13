using MediatR;

namespace Collectibles.Application.Features.CollectibleItems.Queries;

public class GetLinkCachesQuery : IRequest<List<LinkCacheDto>>
{
    public long LinkInfoId { get; set; }
}
