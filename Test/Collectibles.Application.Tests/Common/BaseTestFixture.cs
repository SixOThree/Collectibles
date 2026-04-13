namespace Collectibles.Application.Tests.Common;

public abstract class BaseTestFixture : IDisposable
{
    protected readonly IFixture Fixture;
    protected readonly ApplicationDbContext Context;
    protected readonly CancellationToken CancellationToken = CancellationToken.None;

    protected BaseTestFixture()
    {
        Fixture = new Fixture().Customize(new AutoMoqCustomization());

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Create a mock ICurrentUserService for testing
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.UserId).Returns("test-user-id");
        Context = new ApplicationDbContext(options, currentUserServiceMock.Object);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected async Task<T?> FindAsync<T>(int id)
        where T : BaseEntity
    {
        return await Context.Set<T>().FindAsync(id);
    }

    protected async Task<int> CountAsync<T>()
        where T : BaseEntity
    {
        return await Context.Set<T>().CountAsync();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Context?.Dispose();
        }
    }
}
