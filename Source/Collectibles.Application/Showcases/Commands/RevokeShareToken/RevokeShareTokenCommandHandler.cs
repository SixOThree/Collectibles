using System.Text.Json;

using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Interfaces;
using MediatR;

namespace Collectibles.Application.Showcases.Commands.RevokeShareToken;

public class RevokeShareTokenCommandHandler : IRequestHandler<RevokeShareTokenCommand, bool>
{
    private readonly IShowcaseShareTokenRepository _shareTokenRepository;
    private readonly IEventLogService _eventLogService;

    public RevokeShareTokenCommandHandler(
        IShowcaseShareTokenRepository shareTokenRepository,
        IEventLogService eventLogService)
    {
        _shareTokenRepository = shareTokenRepository;
        _eventLogService = eventLogService;
    }

    public async Task<bool> Handle(RevokeShareTokenCommand request, CancellationToken cancellationToken)
    {
        var token = await _shareTokenRepository.GetByIdAsync(request.TokenId, cancellationToken);

        if (token == null)
        {
            return false;
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
