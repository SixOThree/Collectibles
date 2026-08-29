using System.Security.Cryptography;
using System.Text.Json;

using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Showcases.Commands.GenerateShareLink;

public class GenerateShareLinkCommandHandler : IRequestHandler<GenerateShareLinkCommand, GenerateShareLinkDto>
{
    private readonly IShowcaseShareTokenRepository _shareTokenRepository;
    private readonly IEventLogService _eventLogService;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GenerateShareLinkCommandHandler(
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

    public async Task<GenerateShareLinkDto> Handle(GenerateShareLinkCommand request, CancellationToken cancellationToken)
    {
        var showcase = await _context.Showcases
            .FirstOrDefaultAsync(s => s.Id == request.ShowcaseId, cancellationToken);

        if (showcase == null)
        {
            throw new InvalidOperationException($"Showcase with ID {request.ShowcaseId} not found.");
        }

        if (showcase.UserId != _currentUserService.UserId && !_currentUserService.IsAdministrator)
        {
            throw new UnauthorizedAccessException("You are not authorized to share this showcase.");
        }

        // Generate a unique token
        var token = GenerateUniqueToken();

        // Ensure the token is unique
        while (await _shareTokenRepository.TokenExistsAsync(token, cancellationToken))
        {
            token = GenerateUniqueToken();
        }

        // Create the share token entity
        var shareToken = new ShowcaseShareToken
        {
            ShowcaseId = request.ShowcaseId,
            Token = token,
            ExpiresAt = request.ExpiresAt,
            Note = request.Note,
            IsActive = true,
            ViewCount = 0,
        };

        // Save to database
        await _shareTokenRepository.AddAsync(shareToken, cancellationToken);

        await _eventLogService.LogEventAsync(
            EventAction.Share,
            entityType: "Showcase",
            entityId: request.ShowcaseId,
            entityName: null,
            additionalData: JsonSerializer.Serialize(new { Action = "ShareLinkGenerated", TokenId = shareToken.Id, ExpiresAt = request.ExpiresAt }),
            cancellationToken: cancellationToken);

        // Generate the share URL (relative - will be completed on the client side)
        var shareUrl = $"/share/{token}";

        return new GenerateShareLinkDto
        {
            Token = token,
            ShareUrl = shareUrl,
            ExpiresAt = request.ExpiresAt,
            Created = shareToken.Created ?? DateTime.UtcNow,
        };
    }

    private static string GenerateUniqueToken()
    {
        // Generate a URL-safe random token
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", string.Empty);
    }
}
