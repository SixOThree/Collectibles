using MediatR;

namespace Collectibles.Application.Showcases.Commands.RevokeShareToken;

public class RevokeShareTokenCommand : IRequest<bool>
{
    public long TokenId { get; set; }
}
