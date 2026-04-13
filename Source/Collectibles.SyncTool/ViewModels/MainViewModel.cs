using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;
using Collectibles.SyncTool.Models;
using Collectibles.SyncTool.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Collectibles.SyncTool.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly CollectiblesApiClient _apiClient;
    private readonly FileHashService _fileHashService;
    private readonly SyncComparisonService _comparisonService;
    private readonly SettingsService _settingsService;

    private CancellationTokenSource? _cts;
    private List<SyncItemViewModel> _allItems = [];

    private CancellationTokenSource? _previewCts;
    private readonly LinkedList<(string Key, BitmapImage Image)> _previewCache = new();
    private System.Timers.Timer? _widthSaveTimer;
    private const int PreviewCacheSize = 10;

    public MainViewModel(
        CollectiblesApiClient apiClient,
        FileHashService fileHashService,
        SyncComparisonService comparisonService,
        SettingsService settingsService)
    {
        _apiClient = apiClient;
        _fileHashService = fileHashService;
        _comparisonService = comparisonService;
        _settingsService = settingsService;

        LoadSettings();
    }

    // Connection settings
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CompareCommand))]
    private string _serverUrl = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CompareCommand))]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CompareCommand))]
    private string _showcaseHashId = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CompareCommand))]
    private string _localFolder = string.Empty;

    // State
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CompareCommand))]
    private bool _isOperationRunning;
    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private string? _activeFilter;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _hideMatched;

    // Preview panel
    [ObservableProperty] private bool _isPreviewPanelVisible;
    [ObservableProperty] private BitmapImage? _previewImage;
    [ObservableProperty] private string? _previewFileName;
    [ObservableProperty] private string? _previewFileSize;
    [ObservableProperty] private bool _isPreviewLoading;
    [ObservableProperty] private bool _isActualSize;
    [ObservableProperty] private string _previewPlaceholderText = "Select an image to preview";
    [ObservableProperty] private double _previewPanelWidth = 300;
    [ObservableProperty] private SyncItemViewModel? _selectedPreviewItem;

    // Summary counts
    [ObservableProperty] private int _matchedCount;
    [ObservableProperty] private int _toUploadCount;
    [ObservableProperty] private int _serverOnlyCount;
    [ObservableProperty] private int _movedCopiedCount;
    [ObservableProperty] private int _totalCount;

    public ObservableCollection<SyncItemViewModel> Items { get; } = [];

    public int SelectedUploadCount => Items.Count(i => i.IsSelected && i.Status == SyncStatus.ToUpload);
    public int SelectedDownloadCount => Items.Count(i => i.IsSelected && i.Status == SyncStatus.ServerOnly);
    public int SelectedDeleteCount => Items.Count(i => i.IsSelected && i.Status == SyncStatus.ServerOnly);
    public int SelectedMovedCopiedCount => Items.Count(i => i.IsSelected && i.Status == SyncStatus.MovedCopied);

    partial void OnServerUrlChanged(string value) => SaveSettings();
    partial void OnApiKeyChanged(string value) => SaveSettings();
    partial void OnShowcaseHashIdChanged(string value) => SaveSettings();
    partial void OnLocalFolderChanged(string value) => SaveSettings();
    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnHideMatchedChanged(bool value) => ApplyFilter();

    partial void OnIsPreviewPanelVisibleChanged(bool value)
    {
        SaveSettings();
        if (value && SelectedPreviewItem != null)
        {
            _ = LoadPreviewAsync(SelectedPreviewItem);
        }
    }

    partial void OnPreviewPanelWidthChanged(double value)
    {
        _widthSaveTimer?.Stop();
        _widthSaveTimer?.Dispose();
        _widthSaveTimer = new System.Timers.Timer(500);
        _widthSaveTimer.AutoReset = false;
        _widthSaveTimer.Elapsed += (_, _) =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(SaveSettings);
        };
        _widthSaveTimer.Start();
    }

    partial void OnSelectedPreviewItemChanged(SyncItemViewModel? value)
    {
        IsActualSize = false;
        if (value == null)
        {
            PreviewImage = null;
            PreviewFileName = null;
            PreviewFileSize = null;
            PreviewPlaceholderText = "Select an image to preview";
            return;
        }

        if (IsPreviewPanelVisible)
        {
            _ = LoadPreviewAsync(value);
        }
    }

    partial void OnActiveFilterChanged(string? value) => ApplyFilter();

    [RelayCommand]
    private void BrowseFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select Local Folder",
            InitialDirectory = Directory.Exists(LocalFolder) ? LocalFolder : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };

        if (dialog.ShowDialog() == true)
        {
            LocalFolder = dialog.FolderName;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCompare))]
    private async Task CompareAsync()
    {
        IsOperationRunning = true;
        _cts = new CancellationTokenSource();
        StatusText = "Fetching manifest...";
        ProgressValue = 0;

        try
        {
            _apiClient.Configure(ServerUrl, ApiKey);

            // Fetch server manifest
            var manifest = await _apiClient.GetManifestAsync(ShowcaseHashId, _cts.Token);
            StatusText = $"Hashing local files...";

            // Hash local files
            var progress = new Progress<(int processed, int total)>(p =>
            {
                ProgressValue = (double)p.processed / p.total * 100;
                StatusText = $"Hashing files... ({p.processed}/{p.total})";
            });

            var localFiles = await _fileHashService.HashFilesAsync(LocalFolder, progress, _cts.Token);

            // Compare
            StatusText = "Comparing...";
            var syncItems = _comparisonService.Compare(localFiles, manifest);

            // Populate results
            _allItems = syncItems.Select(i => new SyncItemViewModel(i)).ToList();

            // Subscribe to selection changes
            foreach (var item in _allItems)
            {
                item.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(SyncItemViewModel.IsSelected))
                    {
                        OnPropertyChanged(nameof(SelectedUploadCount));
                        OnPropertyChanged(nameof(SelectedDownloadCount));
                        OnPropertyChanged(nameof(SelectedDeleteCount));
                        OnPropertyChanged(nameof(SelectedMovedCopiedCount));
                    }
                };
            }

            UpdateCounts();
            ActiveFilter = null; // Show all
            ApplyFilter();

            StatusText = $"Comparison complete. {_allItems.Count} files found.";
            ProgressValue = 100;
        }
        catch (OperationCanceledException)
        {
            StatusText = "Operation cancelled.";
        }
        catch (HttpRequestException ex)
        {
            StatusText = ex.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? "Authentication failed — check your API key."
                : $"Network error: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsOperationRunning = false;
        }
    }

    private bool CanCompare() =>
        !string.IsNullOrWhiteSpace(ServerUrl) &&
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(ShowcaseHashId) &&
        !string.IsNullOrWhiteSpace(LocalFolder) &&
        Directory.Exists(LocalFolder) &&
        !IsOperationRunning;

    [RelayCommand]
    private async Task UploadSelectedAsync()
    {
        var toUpload = Items.Where(i => i.IsSelected && i.Status == SyncStatus.ToUpload).ToList();
        if (toUpload.Count == 0)
        {
            return;
        }

        IsOperationRunning = true;
        _cts = new CancellationTokenSource();
        var completed = 0;
        var skipped = 0;

        try
        {
            _apiClient.Configure(ServerUrl, ApiKey);

            var semaphore = new SemaphoreSlim(3); // Max parallel uploads

            var tasks = toUpload.Select(async item =>
            {
                await semaphore.WaitAsync(_cts.Token);
                try
                {
                    var syncItem = item.Item;
                    var relativePath = syncItem.LocalFileName!;

                    // Skip root-level files (relative path must have at least 2 segments)
                    if (relativePath.Split('/', '\\').Length < 2)
                    {
                        Interlocked.Increment(ref skipped);
                        return;
                    }

                    StatusText = $"Uploading {item.Filename}... ({completed + 1}/{toUpload.Count})";

                    var contentType = CollectiblesApiClient.GetContentType(syncItem.LocalFilePath!);
                    var attachmentType = CollectiblesApiClient.GetAttachmentType(syncItem.LocalFilePath!);

                    // Step 1: Initiate sync upload
                    var initiation = await _apiClient.InitiateSyncUploadAsync(
                        ShowcaseHashId, relativePath, syncItem.LocalContentHash!,
                        syncItem.LocalFileSize, contentType, _cts.Token);

                    if (initiation.Skipped)
                    {
                        Interlocked.Increment(ref skipped);
                        Interlocked.Increment(ref completed);
                        ProgressValue = (double)completed / toUpload.Count * 100;
                        return;
                    }

                    // Step 2: Upload blob to Azure
                    var progress = new Progress<double>(_ =>
                    {
                        ProgressValue = (completed + 0.5) / toUpload.Count * 100;
                    });

                    await _apiClient.UploadToAzureAsync(
                        initiation.SasUrl!, syncItem.LocalFilePath!, contentType, progress, _cts.Token);

                    // Step 3: Complete sync upload
                    await _apiClient.CompleteSyncUploadAsync(
                        initiation.UploadId!, initiation.BlobName!, Path.GetFileName(syncItem.LocalFilePath!),
                        contentType, syncItem.LocalFileSize, initiation.TargetItemId!.Value,
                        ShowcaseHashId, syncItem.LocalContentHash,
                        attachmentType.ToString(), _cts.Token);

                    Interlocked.Increment(ref completed);
                    ProgressValue = (double)completed / toUpload.Count * 100;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            StatusText = skipped > 0
                ? $"Uploaded {completed} files ({skipped} skipped as duplicates)."
                : $"Uploaded {completed} files successfully.";
            ProgressValue = 100;

            // Re-run comparison to refresh the list
            await CompareAsync();
        }
        catch (OperationCanceledException)
        {
            StatusText = $"Upload cancelled. {completed}/{toUpload.Count} completed.";
        }
        catch (Exception ex)
        {
            StatusText = $"Upload error: {ex.Message}. {completed}/{toUpload.Count} completed.";
        }
        finally
        {
            IsOperationRunning = false;
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        var toDelete = Items.Where(i => i.IsSelected && i.Status == SyncStatus.ServerOnly).ToList();
        if (toDelete.Count == 0)
        {
            return;
        }

        var fileList = string.Join("\n", toDelete.Select(i => $"  \u2022 {i.Filename}"));
        var dialog = new ConfirmationDialog(
            $"Permanently delete {toDelete.Count} file(s) from the server?",
            fileList,
            "This cannot be undone.")
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        IsOperationRunning = true;
        _cts = new CancellationTokenSource();
        var completed = 0;

        try
        {
            _apiClient.Configure(ServerUrl, ApiKey);

            foreach (var item in toDelete)
            {
                StatusText = $"Deleting {item.Filename}... ({completed + 1}/{toDelete.Count})";

                if (item.AttachmentHashId != null)
                {
                    await _apiClient.DeleteAttachmentAsync(item.AttachmentHashId, _cts.Token);
                }

                completed++;
                ProgressValue = (double)completed / toDelete.Count * 100;
            }

            StatusText = $"Deleted {completed} files.";
            ProgressValue = 100;

            // Re-run comparison to refresh the list
            await CompareAsync();
        }
        catch (OperationCanceledException)
        {
            StatusText = $"Delete cancelled. {completed}/{toDelete.Count} completed.";
        }
        catch (Exception ex)
        {
            StatusText = $"Delete error: {ex.Message}. {completed}/{toDelete.Count} completed.";
        }
        finally
        {
            IsOperationRunning = false;
        }
    }

    [RelayCommand]
    private async Task UploadSingleAsync(SyncItemViewModel item)
    {
        if (item.LocalFilePath == null)
        {
            return;
        }

        IsOperationRunning = true;
        _cts = new CancellationTokenSource();

        try
        {
            _apiClient.Configure(ServerUrl, ApiKey);
            StatusText = $"Uploading {item.Filename}...";
            ProgressValue = 0;

            var syncItem = item.Item;
            var relativePath = syncItem.LocalFileName!;

            // Skip root-level files (relative path must have at least 2 segments)
            if (relativePath.Split('/', '\\').Length < 2)
            {
                StatusText = $"Skipped {item.Filename} — root-level files cannot be uploaded.";
                return;
            }

            var contentType = CollectiblesApiClient.GetContentType(syncItem.LocalFilePath!);
            var attachmentType = CollectiblesApiClient.GetAttachmentType(syncItem.LocalFilePath!);

            // Step 1: Initiate sync upload
            var initiation = await _apiClient.InitiateSyncUploadAsync(
                ShowcaseHashId, relativePath, syncItem.LocalContentHash!,
                syncItem.LocalFileSize, contentType, _cts.Token);

            if (initiation.Skipped)
            {
                StatusText = $"Skipped {item.Filename} — duplicate already exists on server.";
                ProgressValue = 100;
                await CompareAsync();
                return;
            }

            // Step 2: Upload blob to Azure
            var progress = new Progress<double>(p => ProgressValue = p * 100);
            await _apiClient.UploadToAzureAsync(
                initiation.SasUrl!, syncItem.LocalFilePath!, contentType, progress, _cts.Token);

            // Step 3: Complete sync upload
            await _apiClient.CompleteSyncUploadAsync(
                initiation.UploadId!, initiation.BlobName!, Path.GetFileName(syncItem.LocalFilePath!),
                contentType, syncItem.LocalFileSize, initiation.TargetItemId!.Value,
                ShowcaseHashId, syncItem.LocalContentHash,
                attachmentType.ToString(), _cts.Token);

            StatusText = $"Uploaded {item.Filename}.";
            ProgressValue = 100;
            await CompareAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Upload failed: {ex.Message}";
        }
        finally
        {
            IsOperationRunning = false;
        }
    }

    [RelayCommand]
    private async Task DeleteSingleAsync(SyncItemViewModel item)
    {
        if (item.AttachmentHashId == null)
        {
            return;
        }

        var dialog = new ConfirmationDialog(
            $"Permanently delete \"{item.Filename}\" from the server?",
            $"  \u2022 {item.Filename}",
            "This cannot be undone.")
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        IsOperationRunning = true;
        _cts = new CancellationTokenSource();

        try
        {
            _apiClient.Configure(ServerUrl, ApiKey);
            StatusText = $"Deleting {item.Filename}...";

            await _apiClient.DeleteAttachmentAsync(item.AttachmentHashId, _cts.Token);

            StatusText = $"Deleted {item.Filename}.";
            await CompareAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Delete failed: {ex.Message}";
        }
        finally
        {
            IsOperationRunning = false;
        }
    }

    [RelayCommand]
    private async Task DownloadSingleAsync(SyncItemViewModel item)
    {
        if (item.AttachmentHashId == null || item.ServerFilename == null)
        {
            return;
        }

        IsOperationRunning = true;
        _cts = new CancellationTokenSource();

        try
        {
            _apiClient.Configure(ServerUrl, ApiKey);
            StatusText = $"Downloading {item.Filename}...";
            ProgressValue = 0;

            var bytes = await _apiClient.GetAttachmentDownloadAsync(item.AttachmentHashId, _cts.Token);
            if (bytes == null)
            {
                StatusText = $"Download failed: could not fetch {item.Filename}.";
                return;
            }

            var targetPath = BuildDownloadPath(item.Item);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            await File.WriteAllBytesAsync(targetPath, bytes, _cts.Token);

            StatusText = $"Downloaded {item.Filename}.";
            ProgressValue = 100;
            await CompareAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Download failed: {ex.Message}";
        }
        finally
        {
            IsOperationRunning = false;
        }
    }

    [RelayCommand]
    private async Task DownloadSelectedAsync()
    {
        var toDownload = Items.Where(i => i.IsSelected && i.Status == SyncStatus.ServerOnly).ToList();
        if (toDownload.Count == 0)
        {
            return;
        }

        IsOperationRunning = true;
        _cts = new CancellationTokenSource();
        var completed = 0;

        try
        {
            _apiClient.Configure(ServerUrl, ApiKey);

            foreach (var item in toDownload)
            {
                if (item.AttachmentHashId == null || item.ServerFilename == null)
                {
                    continue;
                }

                StatusText = $"Downloading {item.Filename}... ({completed + 1}/{toDownload.Count})";

                var bytes = await _apiClient.GetAttachmentDownloadAsync(item.AttachmentHashId, _cts.Token);
                if (bytes != null)
                {
                    var targetPath = BuildDownloadPath(item.Item);
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                    await File.WriteAllBytesAsync(targetPath, bytes, _cts.Token);
                }

                completed++;
                ProgressValue = (double)completed / toDownload.Count * 100;
            }

            StatusText = $"Downloaded {completed} files.";
            ProgressValue = 100;
            await CompareAsync();
        }
        catch (OperationCanceledException)
        {
            StatusText = $"Download cancelled. {completed}/{toDownload.Count} completed.";
        }
        catch (Exception ex)
        {
            StatusText = $"Download error: {ex.Message}. {completed}/{toDownload.Count} completed.";
        }
        finally
        {
            IsOperationRunning = false;
        }
    }

    [RelayCommand]
    private async Task CopySingleAsync(SyncItemViewModel item)
    {
        if (item.LocalFilePath == null)
        {
            return;
        }

        IsOperationRunning = true;
        _cts = new CancellationTokenSource();

        try
        {
            _apiClient.Configure(ServerUrl, ApiKey);
            StatusText = $"Copying {item.Filename}...";
            ProgressValue = 0;

            var syncItem = item.Item;
            var relativePath = syncItem.LocalFileName!;

            // Skip root-level files (relative path must have at least 2 segments)
            if (relativePath.Split('/', '\\').Length < 2)
            {
                StatusText = $"Skipped {item.Filename} — root-level files cannot be copied.";
                return;
            }

            var contentType = CollectiblesApiClient.GetContentType(syncItem.LocalFilePath!);
            var attachmentType = CollectiblesApiClient.GetAttachmentType(syncItem.LocalFilePath!);

            // Step 1: Initiate sync upload
            var initiation = await _apiClient.InitiateSyncUploadAsync(
                ShowcaseHashId, relativePath, syncItem.LocalContentHash!,
                syncItem.LocalFileSize, contentType, _cts.Token);

            if (initiation.Skipped)
            {
                StatusText = $"Skipped {item.Filename} — duplicate already exists on server.";
                ProgressValue = 100;
                await CompareAsync();
                return;
            }

            // Step 2: Upload blob to Azure
            var progress = new Progress<double>(p => ProgressValue = p * 100);
            await _apiClient.UploadToAzureAsync(
                initiation.SasUrl!, syncItem.LocalFilePath!, contentType, progress, _cts.Token);

            // Step 3: Complete sync upload
            await _apiClient.CompleteSyncUploadAsync(
                initiation.UploadId!, initiation.BlobName!, Path.GetFileName(syncItem.LocalFilePath!),
                contentType, syncItem.LocalFileSize, initiation.TargetItemId!.Value,
                ShowcaseHashId, syncItem.LocalContentHash,
                attachmentType.ToString(), _cts.Token);

            StatusText = $"Copied {item.Filename}.";
            ProgressValue = 100;
            await CompareAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Copy failed: {ex.Message}";
        }
        finally
        {
            IsOperationRunning = false;
        }
    }

    [RelayCommand]
    private async Task MoveSingleAsync(SyncItemViewModel item)
    {
        if (item.LocalFilePath == null || item.AttachmentHashId == null)
        {
            return;
        }

        IsOperationRunning = true;
        _cts = new CancellationTokenSource();

        try
        {
            _apiClient.Configure(ServerUrl, ApiKey);
            StatusText = $"Moving {item.ServerFilename} → {item.Filename}...";
            ProgressValue = 0;

            await _apiClient.MoveAttachmentAsync(
                item.AttachmentHashId, item.Filename, ShowcaseHashId, _cts.Token);

            StatusText = $"Moved {item.Filename}.";
            ProgressValue = 100;
            await CompareAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Move failed: {ex.Message}";
        }
        finally
        {
            IsOperationRunning = false;
        }
    }

    [RelayCommand]
    private async Task CopySelectedAsync()
    {
        var toCopy = Items.Where(i => i.IsSelected && i.Status == SyncStatus.MovedCopied).ToList();
        if (toCopy.Count == 0)
        {
            return;
        }

        IsOperationRunning = true;
        _cts = new CancellationTokenSource();
        var completed = 0;

        try
        {
            _apiClient.Configure(ServerUrl, ApiKey);

            foreach (var item in toCopy)
            {
                if (item.LocalFilePath == null)
                {
                    continue;
                }

                StatusText = $"Copying {item.Filename}... ({completed + 1}/{toCopy.Count})";

                await _apiClient.UploadFileAsync(item.LocalFilePath, ShowcaseHashId, null, _cts.Token);
                completed++;
                ProgressValue = (double)completed / toCopy.Count * 100;
            }

            StatusText = $"Copied {completed} files.";
            ProgressValue = 100;
            await CompareAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Copy error: {ex.Message}. {completed}/{toCopy.Count} completed.";
        }
        finally
        {
            IsOperationRunning = false;
        }
    }

    [RelayCommand]
    private async Task MoveSelectedAsync()
    {
        var toMove = Items.Where(i => i.IsSelected && i.Status == SyncStatus.MovedCopied).ToList();
        if (toMove.Count == 0)
        {
            return;
        }

        IsOperationRunning = true;
        _cts = new CancellationTokenSource();
        var completed = 0;

        try
        {
            _apiClient.Configure(ServerUrl, ApiKey);

            foreach (var item in toMove)
            {
                if (item.LocalFilePath == null || item.AttachmentHashId == null)
                {
                    continue;
                }

                StatusText = $"Moving {item.ServerFilename} → {item.Filename}... ({completed + 1}/{toMove.Count})";

                await _apiClient.MoveAttachmentAsync(
                    item.AttachmentHashId, item.Filename, ShowcaseHashId, _cts.Token);

                completed++;
                ProgressValue = (double)completed / toMove.Count * 100;
            }

            StatusText = $"Moved {completed} files.";
            ProgressValue = 100;
            await CompareAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Move error: {ex.Message}. {completed}/{toMove.Count} completed.";
        }
        finally
        {
            IsOperationRunning = false;
        }
    }

    private static string BuildDataDetailsList(AttachmentContextResponse ctx)
    {
        var parts = new List<string>();
        if (ctx.HasDescription)
        {
            parts.Add("a description");
        }

        if (ctx.HasCustomFields)
        {
            parts.Add("custom fields");
        }

        if (ctx.HasTags)
        {
            parts.Add("tags");
        }

        if (ctx.HasExternalLinks)
        {
            parts.Add("external links");
        }

        if (ctx.HasQrCode)
        {
            parts.Add("a QR code");
        }

        return string.Join(", ", parts);
    }

    [RelayCommand]
    private void SetFilter(string? filter)
    {
        ActiveFilter = ActiveFilter == filter ? null : filter;
    }

    [RelayCommand]
    private void TogglePreviewPanel()
    {
        IsPreviewPanelVisible = !IsPreviewPanelVisible;
    }

    [RelayCommand]
    private void ToggleZoom()
    {
        IsActualSize = !IsActualSize;
    }

    private async Task LoadPreviewAsync(SyncItemViewModel item)
    {
        // Cancel any previous server download
        _previewCts?.Cancel();
        _previewCts = new CancellationTokenSource();
        var ct = _previewCts.Token;

        PreviewFileName = item.Filename;
        PreviewFileSize = item.FileSizeDisplay;

        if (!item.IsPreviewableImage)
        {
            PreviewImage = null;
            PreviewPlaceholderText = "Not an image file";
            IsPreviewLoading = false;
            return;
        }

        // Check LRU cache
        var cacheKey = item.LocalFilePath ?? item.AttachmentHashId ?? item.Filename;
        var cached = _previewCache.FirstOrDefault(c => c.Key == cacheKey);
        if (cached.Image != null)
        {
            // Move to front of LRU
            _previewCache.Remove(cached);
            _previewCache.AddFirst(cached);
            PreviewImage = cached.Image;
            PreviewPlaceholderText = string.Empty;
            IsPreviewLoading = false;
            return;
        }

        if (item.LocalFilePath != null)
        {
            // Load from local file
            await LoadLocalPreviewAsync(item.LocalFilePath, cacheKey);
        }
        else if (item.AttachmentHashId != null)
        {
            // Two-phase server load
            await LoadServerPreviewAsync(item.AttachmentHashId, cacheKey, ct);
        }
        else
        {
            PreviewImage = null;
            PreviewPlaceholderText = "Preview unavailable";
            IsPreviewLoading = false;
        }
    }

    private async Task LoadLocalPreviewAsync(string filePath, string cacheKey)
    {
        IsPreviewLoading = true;
        PreviewPlaceholderText = string.Empty;

        try
        {
            await Task.Run(() =>
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    PreviewImage = bitmap;
                    AddToCache(cacheKey, bitmap);
                });
            });
        }
        catch
        {
            PreviewImage = null;
            PreviewPlaceholderText = "Preview unavailable";
        }
        finally
        {
            IsPreviewLoading = false;
        }
    }

    private async Task LoadServerPreviewAsync(string attachmentHashId, string cacheKey, CancellationToken ct)
    {
        IsPreviewLoading = true;
        PreviewPlaceholderText = string.Empty;

        try
        {
            // Phase 1: Thumbnail
            var thumbnailBytes = await _apiClient.GetAttachmentThumbnailAsync(attachmentHashId, ct);
            if (ct.IsCancellationRequested) return;

            if (thumbnailBytes == null)
            {
                PreviewImage = null;
                PreviewPlaceholderText = "Preview unavailable";
                IsPreviewLoading = false;
                return;
            }

            var thumbnailImage = BytesToBitmapImage(thumbnailBytes);
            PreviewImage = thumbnailImage;

            // Phase 2: Full image (in background)
            var fullBytes = await _apiClient.GetAttachmentDownloadAsync(attachmentHashId, ct);
            if (ct.IsCancellationRequested) return;

            if (fullBytes != null)
            {
                var fullImage = BytesToBitmapImage(fullBytes);
                PreviewImage = fullImage;
                AddToCache(cacheKey, fullImage);
            }
            else
            {
                // Keep thumbnail as the cached version
                AddToCache(cacheKey, thumbnailImage);
            }
        }
        catch (OperationCanceledException)
        {
            // Switching rows — expected
        }
        catch
        {
            PreviewImage = null;
            PreviewPlaceholderText = "Preview unavailable";
        }
        finally
        {
            IsPreviewLoading = false;
        }
    }

    private static BitmapImage BytesToBitmapImage(byte[] bytes)
    {
        var bitmap = new BitmapImage();
        using var ms = new System.IO.MemoryStream(bytes);
        bitmap.BeginInit();
        bitmap.StreamSource = ms;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private void AddToCache(string key, BitmapImage image)
    {
        // Remove if already exists
        var existing = _previewCache.FirstOrDefault(c => c.Key == key);
        if (existing.Key != null)
        {
            _previewCache.Remove(existing);
        }

        _previewCache.AddFirst((key, image));

        // Evict oldest if over capacity
        while (_previewCache.Count > PreviewCacheSize)
        {
            _previewCache.RemoveLast();
        }
    }

    public void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedUploadCount));
        OnPropertyChanged(nameof(SelectedDownloadCount));
        OnPropertyChanged(nameof(SelectedDeleteCount));
        OnPropertyChanged(nameof(SelectedMovedCopiedCount));
    }

    private void ApplyFilter()
    {
        Items.Clear();

        var filtered = _allItems.AsEnumerable();

        if (ActiveFilter != null)
        {
            filtered = ActiveFilter switch
            {
                "ToUpload" => filtered.Where(i => i.Status == SyncStatus.ToUpload),
                "ServerOnly" => filtered.Where(i => i.Status == SyncStatus.ServerOnly),
                "MovedCopied" => filtered.Where(i => i.Status == SyncStatus.MovedCopied),
                "Matched" => filtered.Where(i => i.Status == SyncStatus.Matched),
                _ => filtered,
            };
        }

        if (HideMatched)
        {
            filtered = filtered.Where(i => i.Status != SyncStatus.Matched);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(i =>
                i.Filename.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in filtered)
        {
            Items.Add(item);
        }

        OnPropertyChanged(nameof(SelectedUploadCount));
        OnPropertyChanged(nameof(SelectedDownloadCount));
        OnPropertyChanged(nameof(SelectedDeleteCount));
        OnPropertyChanged(nameof(SelectedMovedCopiedCount));
    }

    private string BuildDownloadPath(SyncItem item)
    {
        var segments = item.ItemPath?.Split(" > ", StringSplitOptions.RemoveEmptyEntries) ?? [];
        var folder = Path.Combine([LocalFolder, .. segments]);
        return Path.Combine(folder, item.ServerFileName ?? "download");
    }

    private void UpdateCounts()
    {
        MatchedCount = _allItems.Count(i => i.Status == SyncStatus.Matched);
        ToUploadCount = _allItems.Count(i => i.Status == SyncStatus.ToUpload);
        ServerOnlyCount = _allItems.Count(i => i.Status == SyncStatus.ServerOnly);
        MovedCopiedCount = _allItems.Count(i => i.Status == SyncStatus.MovedCopied);
        TotalCount = _allItems.Count;
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Load();
        _serverUrl = settings.ServerUrl;
        _apiKey = settings.ApiKey;
        _showcaseHashId = settings.LastShowcaseHashId;
        _localFolder = settings.LastLocalFolder;
        _isPreviewPanelVisible = settings.IsPreviewPanelOpen;
        _previewPanelWidth = settings.PreviewPanelWidth > 0 ? settings.PreviewPanelWidth : 300;
    }

    private void SaveSettings()
    {
        _settingsService.Save(new SyncSettings
        {
            ServerUrl = ServerUrl,
            ApiKey = ApiKey,
            LastShowcaseHashId = ShowcaseHashId,
            LastLocalFolder = LocalFolder,
            IsPreviewPanelOpen = IsPreviewPanelVisible,
            PreviewPanelWidth = PreviewPanelWidth,
        });
    }
}
