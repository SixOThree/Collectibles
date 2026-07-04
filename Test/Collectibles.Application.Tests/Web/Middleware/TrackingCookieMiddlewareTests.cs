using Collectibles.Infrastructure.Services;
using Collectibles.Web.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Collectibles.Application.Tests.Web.Middleware;

public class TrackingCookieMiddlewareTests
{
    [Fact]
    public async Task InvokeAsyncReplacesEmptyTrackingCookieWithUsableTrackingId()
    {
        var sessionTrackingService = new SessionTrackingService();
        var context = CreateContext(sessionTrackingService);
        context.Features.Set<IRequestCookiesFeature>(
            new TestRequestCookiesFeature(
                new TestRequestCookieCollection(new Dictionary<string, string?>
                {
                    ["CollectiblesTrackingId"] = string.Empty,
                })));
        var middleware = new TrackingCookieMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        sessionTrackingService.TrackingId.Should().NotBeNullOrWhiteSpace();
        sessionTrackingService.TrackingId.Should().HaveLength(32);
        context.Response.Headers.SetCookie.ToString().Should().Contain($"CollectiblesTrackingId={sessionTrackingService.TrackingId}");
    }

    [Fact]
    public async Task InvokeAsyncCreatesTrackingCookieWhenCookieFeatureDoesNotContainTrackingCookie()
    {
        var sessionTrackingService = new SessionTrackingService();
        var context = CreateContext(sessionTrackingService);
        context.Features.Set<IRequestCookiesFeature>(
            new TestRequestCookiesFeature(
                new TestRequestCookieCollection(new Dictionary<string, string?>())));
        var middleware = new TrackingCookieMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        sessionTrackingService.TrackingId.Should().NotBeNullOrWhiteSpace();
        sessionTrackingService.TrackingId.Should().HaveLength(32);
        context.Response.Headers.SetCookie.ToString().Should().Contain($"CollectiblesTrackingId={sessionTrackingService.TrackingId}");
    }

    [Fact]
    public async Task InvokeAsyncUsesExistingTrackingCookieWithoutRefreshingExpiration()
    {
        var sessionTrackingService = new SessionTrackingService();
        var context = CreateContext(sessionTrackingService);
        context.Request.Headers.Cookie = "CollectiblesTrackingId=existing-tracking-id";
        var middleware = new TrackingCookieMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        sessionTrackingService.TrackingId.Should().Be("existing-tracking-id");
        context.Response.Headers.SetCookie.ToString().Should().BeEmpty();
    }

    private static DefaultHttpContext CreateContext(ISessionTrackingService sessionTrackingService)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.Setup(x => x.EnvironmentName).Returns(Environments.Development);

        var services = new ServiceCollection()
            .AddSingleton(environment.Object)
            .AddSingleton(sessionTrackingService)
            .BuildServiceProvider();

        return new DefaultHttpContext
        {
            RequestServices = services,
        };
    }

    private sealed class TestRequestCookiesFeature : IRequestCookiesFeature
    {
        public TestRequestCookiesFeature(IRequestCookieCollection cookies)
        {
            Cookies = cookies;
        }

        public IRequestCookieCollection Cookies { get; set; }
    }

    private sealed class TestRequestCookieCollection : IRequestCookieCollection
    {
        private readonly IReadOnlyDictionary<string, string?> _cookies;

        public TestRequestCookieCollection(IReadOnlyDictionary<string, string?> cookies)
        {
            _cookies = cookies;
        }

        public string? this[string key] => _cookies.TryGetValue(key, out var value) ? value : null;

        public int Count => _cookies.Count;

        public ICollection<string> Keys => _cookies.Keys.ToArray();

        public bool ContainsKey(string key)
        {
            return _cookies.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<string, string?>> GetEnumerator()
        {
            return _cookies.GetEnumerator();
        }

        public bool TryGetValue(string key, out string? value)
        {
            return _cookies.TryGetValue(key, out value);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
