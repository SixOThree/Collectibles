using MediatR;

namespace Collectibles.Application.Showcases.Commands.GenerateShareLink;

public class GenerateShareLinkCommand : IRequest<GenerateShareLinkDto>
{
    public long ShowcaseId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Note { get; set; }
}
