using System.Linq.Expressions;

using Collectibles.Application.Interfaces;

using Hangfire;

namespace Collectibles.Infrastructure.Services;

/// <summary>
/// Hangfire-backed <see cref="IBackgroundJobScheduler"/>. Hangfire resolves the target
/// service from its own DI scope when the job runs, so the job never touches services
/// belonging to the scope that queued it.
/// </summary>
public class HangfireBackgroundJobScheduler : IBackgroundJobScheduler
{
    private readonly IBackgroundJobClient _client;

    public HangfireBackgroundJobScheduler(IBackgroundJobClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public void Enqueue<TService>(Expression<Func<TService, Task>> methodCall)
    {
        _client.Enqueue(methodCall);
    }
}
