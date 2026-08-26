using Collectibles.Application.Interfaces;
using Collectibles.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Showcases.Queries.GetShareTokens;

public class GetShareTokensQueryHandler : IRequestHandler<GetShareTokensQuery, List<ShareTokenDto>>
{
    private readonly IShowcaseShareTokenRepository _shareTokenRepository;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetShareTokensQueryHandler(
        IShowcaseShareTokenRepository shareTokenRepository,
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _shareTokenRepository = shareTokenRepository;
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<ShareTokenDto>> Handle(GetShareTokensQuery request, CancellationToken cancellationToken)
    {
        var showcase = await _context.Showcases
            .FirstOrDefaultAsync(s => s.Id == request.ShowcaseId, cancellationToken);

        if (showcase == null || (showcase.UserId != _currentUserService.UserId && !_currentUserService.IsAdministrator))
        {
            throw new UnauthorizedAccessException("You are not authorized to view share links for this showcase.");
        }

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
