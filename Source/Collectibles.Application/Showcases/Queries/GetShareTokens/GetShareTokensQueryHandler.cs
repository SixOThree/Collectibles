using Collectibles.Domain.Interfaces;
using MediatR;

namespace Collectibles.Application.Showcases.Queries.GetShareTokens;

public class GetShareTokensQueryHandler : IRequestHandler<GetShareTokensQuery, List<ShareTokenDto>>
{
    private readonly IShowcaseShareTokenRepository _shareTokenRepository;

    public GetShareTokensQueryHandler(IShowcaseShareTokenRepository shareTokenRepository)
    {
        _shareTokenRepository = shareTokenRepository;
    }

    public async Task<List<ShareTokenDto>> Handle(GetShareTokensQuery request, CancellationToken cancellationToken)
    {
        var tokens = await _shareTokenRepository.GetByShowcaseIdAsync(request.ShowcaseId, cancellationToken);

        return tokens.Select(t => new ShareTokenDto
        {
            Id = t.Id,
            Token = t.Token,
            ShareUrl = $"/share/{t.Token}", // Relative URL
            ExpiresAt = t.ExpiresAt,
            ViewCount = t.ViewCount,
            LastViewedAt = t.LastViewedAt,
            IsActive = t.IsActive,
            IsExpired = t.IsExpired(),
            Note = t.Note,
            Created = t.Created ?? DateTime.UtcNow,
        }).ToList();
    }
}
