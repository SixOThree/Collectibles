namespace Collectibles.Domain.Configuration;

public class PreviewGenerationSettings
{
    public const string SectionName = "PreviewGeneration";

    public bool Images { get; set; } = true;
    public bool Pdf { get; set; } = true;
    public bool Video { get; set; } = true;
    public bool Word { get; set; } = true;
    public bool PowerPoint { get; set; } = true;
}
