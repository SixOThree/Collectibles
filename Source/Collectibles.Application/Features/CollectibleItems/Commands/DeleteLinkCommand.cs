using MediatR;

namespace Collectibles.Application.Features.CollectibleItems.Commands;

public class DeleteLinkCommand : IRequest
{
    public long LinkInfoId { get; set; }
}
