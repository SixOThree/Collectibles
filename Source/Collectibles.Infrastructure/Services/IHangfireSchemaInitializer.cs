namespace Collectibles.Infrastructure.Services;

public interface IHangfireSchemaInitializer
{
    Task EnsureSchemaAsync(CancellationToken cancellationToken = default);
}
