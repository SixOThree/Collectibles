using Collectibles.Application.Tests.Common;
using Collectibles.Infrastructure.Services.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Tests.Infrastructure.Services.Logging;

public class EventLogServiceTests : BaseTestFixture
{
    private readonly Mock<IApplicationDbContextFactory> _contextFactoryMock;

    public EventLogServiceTests()
    {
        _contextFactoryMock = new Mock<IApplicationDbContextFactory>();
        _contextFactoryMock
            .Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new NonDisposableDbContextWrapper(Context as IApplicationDbContext));
    }

    [Fact]
    public async Task GetUserSessionsAsyncBackfillsUserEmailFromLaterSessionEvent()
    {
        var timestamp = DateTime.UtcNow.AddMinutes(-5);
        Context.EventLogs.AddRange(
            new EventLog
            {
                SessionId = "session_tracking-id",
                UserId = "user-1",
                UserEmail = null,
                Action = EventAction.View,
                Timestamp = timestamp,
            },
            new EventLog
            {
                SessionId = "session_tracking-id",
                UserId = "user-1",
                UserEmail = "user@example.com",
                Action = EventAction.Update,
                Timestamp = timestamp.AddMinutes(1),
            });
        await Context.SaveChangesAsync();

        var service = CreateService();

        var sessions = await service.GetUserSessionsAsync(cancellationToken: CancellationToken);

        var session = sessions.Should().ContainSingle().Subject;
        session.UserId.Should().Be("user-1");
        session.UserEmail.Should().Be("user@example.com");
    }

    private EventLogService CreateService()
    {
        return new EventLogService(
            _contextFactoryMock.Object,
            Mock.Of<ICurrentUserService>(),
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<ILogger<EventLogService>>(),
            Mock.Of<ISessionTrackingService>());
    }
}
