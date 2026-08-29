using System.Linq.Expressions;

namespace Collectibles.Application.Interfaces;

/// <summary>
/// Queues work to run outside the current request.
/// </summary>
/// <remarks>
/// Handlers must not launch background work with <c>Task.Run</c>: the captured scoped
/// services (and their <c>DbContext</c>) are disposed when the request scope ends, so the
/// work fails intermittently with <see cref="ObjectDisposedException"/>. The scheduler
/// runs the job in its own scope instead.
/// </remarks>
public interface IBackgroundJobScheduler
{
    /// <summary>
    /// Queues a call on a service that is resolved from a fresh scope when the job runs.
    /// </summary>
    /// <typeparam name="TService">Service type to resolve when the job executes.</typeparam>
    /// <param name="methodCall">The call to make on that service.</param>
    void Enqueue<TService>(Expression<Func<TService, Task>> methodCall);
}
