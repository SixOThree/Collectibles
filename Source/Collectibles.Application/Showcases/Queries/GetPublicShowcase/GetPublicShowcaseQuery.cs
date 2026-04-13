using MediatR;

namespace Collectibles.Application.Showcases.Queries.GetPublicShowcase;

public class GetPublicShowcaseQuery : IRequest<PublicShowcaseDto?>
{
    public string Token { get; set; } = string.Empty;
}
