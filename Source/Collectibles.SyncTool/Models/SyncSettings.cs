namespace Collectibles.SyncTool.Models;

public class SyncSettings
{
    public string ServerUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string LastShowcaseHashId { get; set; } = string.Empty;
    public string LastLocalFolder { get; set; } = string.Empty;
    public int MaxParallelUploads { get; set; } = 3;
    public bool IsPreviewPanelOpen { get; set; }
    public double PreviewPanelWidth { get; set; } = 300;
}
