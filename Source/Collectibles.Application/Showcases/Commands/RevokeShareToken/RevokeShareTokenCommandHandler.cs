using System.Text.Json;

using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Showcases.Commands.RevokeShareToken;

public class RevokeShareTokenCommandHandler : IRequestHandler<RevokeShareTokenCommand, bool>
{
    private readonly IShowcaseShareTokenRepository _shareTokenRepository;
    private readonly IEventLogService _eventLogService;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RevokeShareTokenCommandHandler(
        IShowcaseShareTokenRepository shareTokenRepository,
        IEventLogService eventLogService,
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _shareTokenRepository = shareTokenRepository;
        _eventLogService = eventLogService;
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(RevokeShareTokenCommand request, CancellationToken cancellationToken)
    {
        var token = await _shareTokenRepository.GetByIdAsync(request.TokenId, cancellationToken);

        if (token == null)
        {
            return false;
        }

        var showcase = await _context.Showcases
            .FirstOrDefaultAsync(s => s.Id == token.ShowcaseId, cancellationToken);

        if (showcase == null || (showcase.UserId != _currentUserService.UserId && !_currentUserService.IsAdministrator))
        {
            throw new UnauthorizedAccessException("You are not authorized to revoke share links for this showcase.");
        }

        token.IsActive = false;
        await _shareTokenRepository.UpdateAsync(token, cancellationToken);

        await _eventLogService.LogEventAsync(
            EventAction.Share,
            entityType: "Showcase",
            entityId: token.ShowcaseId,
            entityName: null,
            additionalData: JsonSerializer.Serialize(new { Action = "ShareTokenRevoked", TokenId = request.TokenId }),
            cancellationToken: cancellationToken);

        return true;
    }
}
