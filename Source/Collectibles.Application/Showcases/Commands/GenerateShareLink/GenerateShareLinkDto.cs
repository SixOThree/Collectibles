namespace Collectibles.Application.Showcases.Commands.GenerateShareLink;

public class GenerateShareLinkDto
{
    public string Token { get; set; } = string.Empty;
    public string ShareUrl { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public DateTime Created { get; set; }
}
