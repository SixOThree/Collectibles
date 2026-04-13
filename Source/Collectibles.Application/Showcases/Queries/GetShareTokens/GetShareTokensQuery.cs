using MediatR;

namespace Collectibles.Application.Showcases.Queries.GetShareTokens;

public class GetShareTokensQuery : IRequest<List<ShareTokenDto>>
{
    public long ShowcaseId { get; set; }
}
