using System.Windows.Media;

using Collectibles.SyncTool.Models;

using CommunityToolkit.Mvvm.ComponentModel;

namespace Collectibles.SyncTool.ViewModels;

public partial class SyncItemViewModel : ObservableObject
{
    private readonly SyncItem _item;

    public SyncItemViewModel(SyncItem item)
    {
        _item = item;
    }

    [ObservableProperty]
    private bool _isSelected;

    public SyncItem Item => _item;

    public SyncStatus Status => _item.Status;
    public string Filename => _item.DisplayFileName;
    public string? ServerFilename => _item.ServerFileName;
    public string? ItemPath => _item.ItemPath;
    public long FileSize => _item.DisplayFileSize;
    public string? AttachmentHashId => _item.AttachmentHashId;
    public string? LocalFilePath => _item.LocalFilePath;

    public string StatusIcon => _item.Status switch
    {
        SyncStatus.Matched => "\u2713",    // ✓
        SyncStatus.ToUpload => "\u2191",   // ↑
        SyncStatus.ServerOnly => "\U0001F5D1", // 🗑
        SyncStatus.MovedCopied => "\u2194", // ↔
        _ => "?",
    };

    public Brush StatusBrush => _item.Status switch
    {
        SyncStatus.Matched => new SolidColorBrush(Color.FromRgb(0x6C, 0x70, 0x86)),    // gray
        SyncStatus.ToUpload => new SolidColorBrush(Color.FromRgb(0xA6, 0xE3, 0xA1)),   // green
        SyncStatus.ServerOnly => new SolidColorBrush(Color.FromRgb(0xE0, 0x8C, 0x56)),  // orange
        SyncStatus.MovedCopied => new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6)), // blue
        _ => Brushes.White,
    };

    public double RowOpacity => _item.Status == SyncStatus.Matched ? 0.5 : 1.0;

    public string ActionLabel => _item.Status switch
    {
        SyncStatus.ToUpload => "Upload",
        _ => string.Empty,
    };

    public bool HasAction => _item.Status == SyncStatus.ToUpload;

    public bool HasServerOnlyActions => _item.Status == SyncStatus.ServerOnly;

    public bool HasCopyMoveActions => _item.Status == SyncStatus.MovedCopied;

    public string FileSizeDisplay => FormatFileSize(_item.DisplayFileSize);

    public bool HasLocalFile => _item.LocalFilePath != null;

    private static readonly HashSet<string> PreviewableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".tiff", ".tif",
    };

    public bool IsPreviewableImage
    {
        get
        {
            var fileName = _item.LocalFileName ?? _item.ServerFileName;
            if (fileName == null)
            {
                return false;
            }

            var ext = System.IO.Path.GetExtension(fileName);
            return PreviewableExtensions.Contains(ext);
        }
    }

    public string MovedCopiedInfo => _item.Status == SyncStatus.MovedCopied && _item.ServerFileName != null
        ? $"\u2192 was: {_item.ServerFileName}"
        : string.Empty;

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024.0:F0} KB";
        }

        if (bytes < 1024 * 1024 * 1024)
        {
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }

        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }
}
