using Collectibles.Domain.Configuration.Storage;
using Collectibles.Domain.Enums;
using Collectibles.Domain.Interfaces;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Collectibles.Infrastructure.FileStorage;

public interface IFileStorageFactory
{
    IFileStorage CreateFileStorage();
}

public class FileStorageFactory : IFileStorageFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly StorageSettings _storageSettings;

    // The providers are stateless and thread-safe (the database provider resolves its own
    // scope per call), so a single instance is shared instead of being rebuilt per DI scope.
    // This also keeps the Azure container-existence check to one call at first use.
    private readonly Lazy<IFileStorage> _fileStorage;

    public FileStorageFactory(IServiceProvider serviceProvider, IOptions<StorageSettings> storageOptions)
    {
        _serviceProvider = serviceProvider;
        _storageSettings = storageOptions.Value;
        _fileStorage = new Lazy<IFileStorage>(CreateProvider, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public IFileStorage CreateFileStorage() => _fileStorage.Value;

    private IFileStorage CreateProvider()
    {
        return _storageSettings.Provider switch
        {
            StorageProvider.Database => new ScopedDatabaseFileStorage(_serviceProvider.GetRequiredService<IServiceScopeFactory>()),
            StorageProvider.AzureBlobStorage => new AzureBlobFileStorage(Options.Create(_storageSettings), _serviceProvider.GetRequiredService<ILogger<AzureBlobFileStorage>>()),
            StorageProvider.LocalFileSystem => new LocalFileStorage(Options.Create(_storageSettings)),
            _ => throw new NotSupportedException($"Storage provider {_storageSettings.Provider} is not supported"),
        };
    }
}
