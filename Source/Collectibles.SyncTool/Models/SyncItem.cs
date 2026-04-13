using Collectibles.Domain.Common.Enums;

namespace Collectibles.SyncTool.Models;

public class SyncItem
{
    // Local file info (null for server-only items)
    public string? LocalFilePath { get; init; }
    public string? LocalFileName { get; init; }
    public string? LocalContentHash { get; init; }
    public long LocalFileSize { get; init; }

    // Server manifest info (null for upload-only items)
    public string? ServerFileName { get; init; }
    public string? ServerContentHash { get; init; }
    public long ServerFileSize { get; init; }
    public string? ItemPath { get; init; }
    public string? AttachmentHashId { get; init; }
    public AttachmentType? AttachmentType { get; init; }

    // Classification
    public SyncStatus Status { get; init; }

    /// <summary>
    /// Display filename — local name for local files, server name for server-only.
    /// </summary>
    public string DisplayFileName => LocalFileName ?? ServerFileName ?? "(unknown)";

    /// <summary>
    /// Display file size — local size for local files, server size for server-only.
    /// </summary>
    public long DisplayFileSize => LocalFileSize > 0 ? LocalFileSize : ServerFileSize;
}
