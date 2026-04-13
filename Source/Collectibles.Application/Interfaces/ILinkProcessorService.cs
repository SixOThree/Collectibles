namespace Collectibles.Application.Interfaces;

public interface ILinkProcessorService
{
    Task ProcessPendingLinks(CancellationToken cancellationToken);
}
