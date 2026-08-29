using Collectibles.Domain.Configuration.Storage;
using Collectibles.Domain.Enums;
using Collectibles.Infrastructure.FileStorage;

using Microsoft.Extensions.Options;

namespace Collectibles.Application.Tests.Infrastructure;

/// <summary>
/// LocalFileSystem is the default storage provider, and upload commands pass the client's
/// file name straight through. These tests pin the containment guarantee: nothing a caller
/// supplies may produce a write outside the configured storage root.
/// </summary>
public class LocalFileStorageContainmentTests : IDisposable
{
    private readonly string _root;
    private readonly string _outsideRoot;
    private readonly LocalFileStorage _storage;

    public LocalFileStorageContainmentTests()
    {
        var sandbox = Path.Combine(Path.GetTempPath(), "collectibles-storage-tests", Guid.NewGuid().ToString("N"));
        _root = Path.Combine(sandbox, "root");
        _outsideRoot = Path.Combine(sandbox, "outside");
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(_outsideRoot);

        _storage = new LocalFileStorage(Options.Create(new StorageSettings
        {
            Provider = StorageProvider.LocalFileSystem,
            LocalFileSystem = new LocalFileSystemSettings
            {
                BasePath = _root,
                UseAbsolutePath = true,
            },
        }));
    }

    [Theory]
    [InlineData("a/../../b/evil.bin")]
    [InlineData(@"..\..\..\x\evil.zip")]
    [InlineData("../outside/evil.bin")]
    public async Task SaveFileAsyncRejectsTraversalInTheDirectoryPortion(string fileName)
    {
        var act = async () => await _storage.SaveFileAsync("payload"u8.ToArray(), fileName, "application/octet-stream");

        await act.Should().ThrowAsync<ArgumentException>();
        Directory.GetFiles(_outsideRoot, "*", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public async Task SaveFileAsyncRejectsRootedPaths()
    {
        var rooted = Path.Combine(_outsideRoot, "evil.bin");

        var act = async () => await _storage.SaveFileAsync("payload"u8.ToArray(), rooted, "application/octet-stream");

        await act.Should().ThrowAsync<ArgumentException>();
        File.Exists(rooted).Should().BeFalse();
    }

    [Fact]
    public async Task SaveFileAsyncKeepsLegitimateDirectoryStructureInsideTheRoot()
    {
        var relativePath = await _storage.SaveFileAsync("payload"u8.ToArray(), "photos/holiday.jpg", "image/jpeg", showcaseId: 7);

        relativePath.Should().StartWith("7");
        var fullPath = Path.Combine(_root, relativePath);
        File.Exists(fullPath).Should().BeTrue();
        Path.GetFullPath(fullPath).Should().StartWith(Path.GetFullPath(_root));
    }

    [Fact]
    public async Task ReadPathsRefuseToEscapeTheRoot()
    {
        var outsideFile = Path.Combine(_outsideRoot, "secret.txt");
        await File.WriteAllTextAsync(outsideFile, "secret");

        (await _storage.GetFileAsync("../outside/secret.txt")).Should().BeNull();
        (await _storage.FileExistsAsync("../outside/secret.txt")).Should().BeFalse();
        (await _storage.GetFileSizeAsync("../outside/secret.txt")).Should().BeNull();
        (await _storage.GetFileStreamAsync("../outside/secret.txt")).Should().BeNull();

        await _storage.DeleteFileAsync("../outside/secret.txt");
        File.Exists(outsideFile).Should().BeTrue();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try
        {
            var sandbox = Directory.GetParent(_root)?.FullName;
            if (sandbox is not null && Directory.Exists(sandbox))
            {
                Directory.Delete(sandbox, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best effort cleanup of the temp sandbox.
        }
    }
}
