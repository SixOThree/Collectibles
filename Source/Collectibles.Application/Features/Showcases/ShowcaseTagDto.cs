namespace Collectibles.Application.Features.Showcases;

/// <summary>
/// Data transfer object for showcase tag information with usage count.
/// </summary>
public class ShowcaseTagDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ShowcaseCount { get; set; }
}
