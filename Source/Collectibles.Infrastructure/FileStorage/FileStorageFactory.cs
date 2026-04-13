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

    public FileStorageFactory(IServiceProvider serviceProvider, IOptions<StorageSettings> storageOptions)
    {
        _serviceProvider = serviceProvider;
        _storageSettings = storageOptions.Value;
    }

    public IFileStorage CreateFileStorage()
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
