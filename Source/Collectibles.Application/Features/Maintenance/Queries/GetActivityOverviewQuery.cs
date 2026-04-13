using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Maintenance.Queries;

public record DailyActivityDto
{
    public DateTime Date { get; init; }
    public int ItemsCreated { get; init; }
    public int AttachmentsUploaded { get; init; }
    public int UserLogins { get; init; }
    public int UniqueAuthenticatedUsers { get; init; }
    public int AnonymousRequests { get; init; }
    public int Errors { get; init; }
    public int TotalRequests { get; init; }
}

public record ActivityOverviewDto
{
    public IReadOnlyList<DailyActivityDto> Days { get; init; } = Array.Empty<DailyActivityDto>();
}

public record GetActivityOverviewQuery(int Days = 7) : IRequest<ActivityOverviewDto>;

public class GetActivityOverviewQueryHandler : IRequestHandler<GetActivityOverviewQuery, ActivityOverviewDto>
{
    private readonly IApplicationDbContextFactory _contextFactory;

    public GetActivityOverviewQueryHandler(IApplicationDbContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<ActivityOverviewDto> Handle(GetActivityOverviewQuery request, CancellationToken cancellationToken)
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.Date.AddDays(-(request.Days - 1));

        // Each parallel query needs its own DbContext (EF Core contexts are not thread-safe)
        var eventTask = RunWithContext(ctx => GetEventStats(ctx, startDate, endDate, cancellationToken), cancellationToken);
        var uniqueUsersTask = RunWithContext(ctx => GetUniqueUserStats(ctx, startDate, endDate, cancellationToken), cancellationToken);
        var errorTask = RunWithContext(ctx => GetErrorStats(ctx, startDate, endDate, cancellationToken), cancellationToken);
        var requestTask = RunWithContext(ctx => GetRequestStats(ctx, startDate, endDate, cancellationToken), cancellationToken);

        await Task.WhenAll(eventTask, uniqueUsersTask, errorTask, requestTask);

        var eventStats = eventTask.Result.ToDictionary(e => e.Date);
        var uniqueUserStats = uniqueUsersTask.Result.ToDictionary(u => u.Date);
        var errorStats = errorTask.Result.ToDictionary(e => e.Date);
        var requestStats = requestTask.Result.ToDictionary(r => r.Date);

        // Build a complete list with zero-fill for missing days
        var days = Enumerable.Range(0, request.Days)
            .Select(offset =>
            {
                var date = startDate.AddDays(offset);
                eventStats.TryGetValue(date, out var evt);
                uniqueUserStats.TryGetValue(date, out var usr);
                errorStats.TryGetValue(date, out var err);
                requestStats.TryGetValue(date, out var req);

                return new DailyActivityDto
                {
                    Date = date,
                    ItemsCreated = evt?.ItemsCreated ?? 0,
                    AttachmentsUploaded = evt?.AttachmentsUploaded ?? 0,
                    UserLogins = evt?.UserLogins ?? 0,
                    UniqueAuthenticatedUsers = usr?.Count ?? 0,
                    Errors = err?.Count ?? 0,
                    AnonymousRequests = req?.AnonymousRequests ?? 0,
                    TotalRequests = req?.TotalRequests ?? 0,
                };
            })
            .ToList();

        return new ActivityOverviewDto { Days = days };
    }

    private async Task<T> RunWithContext<T>(Func<IApplicationDbContext, Task<T>> work, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await work(context);
    }

    private static async Task<List<EventDayStat>> GetEventStats(
        IApplicationDbContext context, DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        return await context.EventLogs
            .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
            .GroupBy(e => e.Timestamp.Date)
            .Select(g => new EventDayStat
            {
                Date = g.Key,
                ItemsCreated = g.Count(e => e.Action == EventAction.Create && e.EntityType == "CollectibleItem"),
                AttachmentsUploaded = g.Count(e => e.Action == EventAction.Upload),
                UserLogins = g.Count(e => e.Action == EventAction.Login),
            })
            .ToListAsync(ct);
    }

    private static async Task<List<UniqueUserDayStat>> GetUniqueUserStats(
        IApplicationDbContext context, DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        // Separate query for distinct user count per day — avoids EF Core translation issues with Distinct().Count() inside GroupBy
        return await context.EventLogs
            .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate && e.UserId != null)
            .Select(e => new { Date = e.Timestamp.Date, e.UserId })
            .Distinct()
            .GroupBy(x => x.Date)
            .Select(g => new UniqueUserDayStat { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);
    }

    private static async Task<List<ErrorDayStat>> GetErrorStats(
        IApplicationDbContext context, DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        return await context.SysLogs
            .Where(s => s.Timestamp >= startDate && s.Timestamp <= endDate && s.Level >= LogLevel.Error)
            .GroupBy(s => s.Timestamp.Date)
            .Select(g => new ErrorDayStat { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);
    }

    private static async Task<List<RequestDayStat>> GetRequestStats(
        IApplicationDbContext context, DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        return await context.RequestLogs
            .Where(r => r.Timestamp >= startDate && r.Timestamp <= endDate)
            .GroupBy(r => r.Timestamp.Date)
            .Select(g => new RequestDayStat
            {
                Date = g.Key,
                TotalRequests = g.Count(),
                AnonymousRequests = g.Count(r => r.UserId == null),
            })
            .ToListAsync(ct);
    }

    private class EventDayStat
    {
        public DateTime Date { get; init; }
        public int ItemsCreated { get; init; }
        public int AttachmentsUploaded { get; init; }
        public int UserLogins { get; init; }
    }

    private class UniqueUserDayStat
    {
        public DateTime Date { get; init; }
        public int Count { get; init; }
    }

    private class ErrorDayStat
    {
        public DateTime Date { get; init; }
        public int Count { get; init; }
    }

    private class RequestDayStat
    {
        public DateTime Date { get; init; }
        public int TotalRequests { get; init; }
        public int AnonymousRequests { get; init; }
    }
}
