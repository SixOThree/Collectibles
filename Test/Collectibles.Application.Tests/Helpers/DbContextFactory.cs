namespace Collectibles.Application.Tests.Helpers;

public static class DbContextFactory
{
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Create a mock ICurrentUserService for testing
        var currentUserService = Mock.Of<ICurrentUserService>();
        var context = new ApplicationDbContext(options, currentUserService);
        context.Database.EnsureCreated();

        return context;
    }

    public static ApplicationDbContext CreateWithData(params BaseEntity[] entities)
    {
        var context = Create();

        if (entities.Length != 0)
        {
            context.AddRange(entities);
            context.SaveChanges();
        }

        return context;
    }

    public static void Destroy(ApplicationDbContext context)
    {
        context.Database.EnsureDeleted();
        context.Dispose();
    }
}
