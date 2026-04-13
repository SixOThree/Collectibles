namespace Collectibles.Domain.Configuration;

public class ExternalLinksOptions
{
    public const string SectionName = "ExternalLinks";

    public bool Enabled { get; set; } = true;
    public bool CachingEnabled { get; set; } = true;
}
