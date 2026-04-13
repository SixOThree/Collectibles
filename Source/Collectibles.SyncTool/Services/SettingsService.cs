using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Collectibles.SyncTool.Models;

namespace Collectibles.SyncTool.Services;

public class SettingsService
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Collectibles", "SyncTool");

    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public SyncSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return new SyncSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<SettingsDto>(json, JsonOptions);
            if (settings == null)
            {
                return new SyncSettings();
            }

            return new SyncSettings
            {
                ServerUrl = settings.ServerUrl ?? string.Empty,
                ApiKey = DecryptApiKey(settings.EncryptedApiKey),
                LastShowcaseHashId = settings.LastShowcaseHashId ?? string.Empty,
                LastLocalFolder = settings.LastLocalFolder ?? string.Empty,
                MaxParallelUploads = settings.MaxParallelUploads > 0 ? settings.MaxParallelUploads : 3,
                IsPreviewPanelOpen = settings.IsPreviewPanelOpen,
                PreviewPanelWidth = settings.PreviewPanelWidth > 0 ? settings.PreviewPanelWidth : 300,
            };
        }
        catch
        {
            return new SyncSettings();
        }
    }

    public void Save(SyncSettings settings)
    {
        Directory.CreateDirectory(SettingsDir);

        var dto = new SettingsDto
        {
            ServerUrl = settings.ServerUrl,
            EncryptedApiKey = EncryptApiKey(settings.ApiKey),
            LastShowcaseHashId = settings.LastShowcaseHashId,
            LastLocalFolder = settings.LastLocalFolder,
            MaxParallelUploads = settings.MaxParallelUploads,
            IsPreviewPanelOpen = settings.IsPreviewPanelOpen,
            PreviewPanelWidth = settings.PreviewPanelWidth,
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }

    private static string EncryptApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(apiKey);
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    private static string DecryptApiKey(string? encrypted)
    {
        if (string.IsNullOrEmpty(encrypted))
        {
            return string.Empty;
        }

        try
        {
            var bytes = Convert.FromBase64String(encrypted);
            var decrypted = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return string.Empty;
        }
    }

    private class SettingsDto
    {
        public string? ServerUrl { get; set; }
        public string? EncryptedApiKey { get; set; }
        public string? LastShowcaseHashId { get; set; }
        public string? LastLocalFolder { get; set; }
        public int MaxParallelUploads { get; set; } = 3;
        public bool IsPreviewPanelOpen { get; set; }
        public double PreviewPanelWidth { get; set; } = 300;
    }
}
