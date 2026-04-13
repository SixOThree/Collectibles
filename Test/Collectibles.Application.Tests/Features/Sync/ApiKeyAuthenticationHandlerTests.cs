using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration;
using Collectibles.Infrastructure.Persistence;
using Collectibles.Web.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Collectibles.Application.Tests.Features.Sync;

public class ApiKeyAuthenticationHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IApiKeyService> _apiKeyServiceMock;
    private readonly Mock<IOptionsMonitor<SyncToolSettings>> _syncToolSettingsMock;

    public ApiKeyAuthenticationHandlerTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _apiKeyServiceMock = new Mock<IApiKeyService>();
        _syncToolSettingsMock = new Mock<IOptionsMonitor<SyncToolSettings>>();
        _syncToolSettingsMock.Setup(x => x.CurrentValue)
            .Returns(new SyncToolSettings { Enabled = true });

        // Default to empty user set for tests that don't call SetupUserLookup
        SetupEmptyUserLookup();
    }

    private void SetupEmptyUserLookup()
    {
        var emptyUsers = new List<ApplicationUser>().AsQueryable();
        var mockSet = new Mock<DbSet<ApplicationUser>>();
        mockSet.As<IAsyncEnumerable<ApplicationUser>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<ApplicationUser>(emptyUsers.GetEnumerator()));
        mockSet.As<IQueryable<ApplicationUser>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<ApplicationUser>(emptyUsers.Provider));
        mockSet.As<IQueryable<ApplicationUser>>()
            .Setup(m => m.Expression).Returns(emptyUsers.Expression);
        mockSet.As<IQueryable<ApplicationUser>>()
            .Setup(m => m.ElementType).Returns(emptyUsers.ElementType);
        mockSet.As<IQueryable<ApplicationUser>>()
            .Setup(m => m.GetEnumerator()).Returns(emptyUsers.GetEnumerator());
        _userManagerMock.Setup(x => x.Users).Returns(mockSet.Object);
    }

    [Fact]
    public async Task ShouldReturnNoResultWhenNoApiKeyHeader()
    {
        var context = new DefaultHttpContext();
        var handler = await CreateAndInitializeHandler(context);

        var result = await handler.AuthenticateAsync();

        result.None.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldFailWhenSyncToolDisabledGlobally()
    {
        _syncToolSettingsMock.Setup(x => x.CurrentValue)
            .Returns(new SyncToolSettings { Enabled = false });

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "some-key";
        var handler = await CreateAndInitializeHandler(context);

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldFailWhenNoUserMatchesKeyHash()
    {
        _apiKeyServiceMock.Setup(x => x.HashKey("test-key")).Returns("HASH123");

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "test-key";
        var handler = await CreateAndInitializeHandler(context);

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldFailWhenUserSyncToolDisabled()
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            Email = "test@example.com",
            DisplayName = "Test User",
            SyncToolEnabled = false,
            IsActive = true,
            ApiKeyHash = "HASH123"
        };
        _apiKeyServiceMock.Setup(x => x.HashKey("test-key")).Returns("HASH123");
        SetupUserLookup(user);

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "test-key";
        var handler = await CreateAndInitializeHandler(context);

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldFailWhenUserIsInactive()
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            Email = "test@example.com",
            DisplayName = "Test User",
            SyncToolEnabled = true,
            IsActive = false,
            ApiKeyHash = "HASH123"
        };
        _apiKeyServiceMock.Setup(x => x.HashKey("test-key")).Returns("HASH123");
        SetupUserLookup(user);

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "test-key";
        var handler = await CreateAndInitializeHandler(context);

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldSucceedForValidKeyWithEnabledActiveUser()
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            Email = "test@example.com",
            DisplayName = "Test User",
            SyncToolEnabled = true,
            IsActive = true,
            ApiKeyHash = "HASH123"
        };
        _apiKeyServiceMock.Setup(x => x.HashKey("test-key")).Returns("HASH123");
        SetupUserLookup(user);

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "test-key";
        var handler = await CreateAndInitializeHandler(context);

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.Principal!.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be("user-1");
        result.Principal!.FindFirst(ClaimTypes.Name)!.Value.Should().Be("Test User");
    }

    private void SetupUserLookup(ApplicationUser user)
    {
        var users = new List<ApplicationUser> { user }.AsQueryable();
        var mockSet = new Mock<DbSet<ApplicationUser>>();
        mockSet.As<IAsyncEnumerable<ApplicationUser>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<ApplicationUser>(users.GetEnumerator()));
        mockSet.As<IQueryable<ApplicationUser>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<ApplicationUser>(users.Provider));
        mockSet.As<IQueryable<ApplicationUser>>()
            .Setup(m => m.Expression).Returns(users.Expression);
        mockSet.As<IQueryable<ApplicationUser>>()
            .Setup(m => m.ElementType).Returns(users.ElementType);
        mockSet.As<IQueryable<ApplicationUser>>()
            .Setup(m => m.GetEnumerator()).Returns(users.GetEnumerator());

        _userManagerMock.Setup(x => x.Users).Returns(mockSet.Object);
    }

    private async Task<ApiKeyAuthenticationHandler> CreateAndInitializeHandler(HttpContext context)
    {
        var options = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        options.Setup(x => x.Get(It.IsAny<string>()))
            .Returns(new AuthenticationSchemeOptions());
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        var encoder = UrlEncoder.Default;

        var handler = new ApiKeyAuthenticationHandler(
            options.Object,
            loggerFactory.Object,
            encoder,
            _userManagerMock.Object,
            _apiKeyServiceMock.Object,
            _syncToolSettingsMock.Object);

        var scheme = new AuthenticationScheme(
            ApiKeyAuthenticationHandler.SchemeName,
            null,
            typeof(ApiKeyAuthenticationHandler));

        await handler.InitializeAsync(scheme, context);
        return handler;
    }
}

internal class TestAsyncQueryProvider<T> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

    public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<T>(expression);
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);
    public object? Execute(Expression expression) => _inner.Execute(expression);
    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);
    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var result = Execute(expression);
        var expectedResultType = typeof(TResult).GetGenericArguments()[0];
        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(expectedResultType)
            .Invoke(null, [result])!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(Expression expression) : base(expression) { }
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
    public T Current => _inner.Current;
    public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());
    public ValueTask DisposeAsync() { _inner.Dispose(); return default; }
}
