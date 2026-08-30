using System.Security.Cryptography;
using System.Text.Json;

using Collectibles.Application.Interfaces;
using Collectibles.Domain.Common;
using Collectibles.Domain.Constants;
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

        var expiresAt = ResolveExpiry(request.ExpiresAt);

        // Create the share token entity. Only the hash is persisted: the plaintext leaves this
        // method in the returned URL and is never recoverable from storage afterwards.
        var shareToken = new ShowcaseShareToken
        {
            ShowcaseId = request.ShowcaseId,
            TokenHash = ShareTokenHash.Compute(token),
            ExpiresAt = expiresAt,
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
            additionalData: JsonSerializer.Serialize(new { Action = "ShareLinkGenerated", TokenId = shareToken.Id, ExpiresAt = expiresAt }),
            cancellationToken: cancellationToken);

        // Generate the share URL (relative - will be completed on the client side)
        var shareUrl = $"/share/{token}";

        return new GenerateShareLinkDto
        {
            Token = token,
            ShareUrl = shareUrl,
            ExpiresAt = expiresAt,
            Created = shareToken.Created ?? DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Applies the default expiry when the caller chose none, and clamps anything further out than
    /// the maximum window. A share link is a bearer credential, so it always ages out.
    /// </summary>
    private static DateTime ResolveExpiry(DateTime? requested)
    {
        var now = DateTime.UtcNow;
        var latest = now.AddDays(ApplicationConstants.ValidationLengths.ShareTokenMaxExpiryDays);

        if (requested is not { } value)
        {
            return now.AddDays(ApplicationConstants.ValidationLengths.ShareTokenDefaultExpiryDays);
        }

        var utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();

        // A date already in the past would create a link that never worked.
        if (utc <= now)
        {
            return now.AddDays(ApplicationConstants.ValidationLengths.ShareTokenDefaultExpiryDays);
        }

        return utc > latest ? latest : utc;
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
