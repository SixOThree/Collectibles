using Collectibles.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Collectibles.Application.Tests.Features.Users.Commands;

public class DeleteUserCommandServiceTests
{
    [Fact]
    public async Task DeleteUserAsyncShouldDeleteUserWhenAnotherContextUpdatedConcurrencyStamp()
    {
        var databaseName = Guid.NewGuid().ToString();

        await using var primaryContext = CreateContext(databaseName);
        await using var secondaryContext = CreateContext(databaseName);

        using var primaryUserManager = CreateUserManager(primaryContext);
        using var secondaryUserManager = CreateUserManager(secondaryContext);

        var service = new UserManagementService(primaryUserManager, CreateRoleManager(), primaryContext);

        var user = new ApplicationUser
        {
            UserName = "stale-user@example.com",
            Email = "stale-user@example.com",
            EmailConfirmed = true,
        };

        var createResult = await primaryUserManager.CreateAsync(user);
        createResult.Succeeded.Should().BeTrue();

        var trackedUser = await primaryUserManager.FindByIdAsync(user.Id);
        trackedUser.Should().NotBeNull();

        var updatedUser = await secondaryUserManager.FindByIdAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.ConcurrencyStamp = Guid.NewGuid().ToString("N");

        var updateResult = await secondaryUserManager.UpdateAsync(updatedUser);
        updateResult.Succeeded.Should().BeTrue();

        var act = async () => await service.DeleteUserAsync(user.Id);

        await act.Should().NotThrowAsync();

        await using var verificationContext = CreateContext(databaseName);
        using var verificationUserManager = CreateUserManager(verificationContext);
        var deletedUser = await verificationUserManager.FindByIdAsync(user.Id);
        deletedUser.Should().BeNull();
    }

    private static ApplicationDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.UserId).Returns("test-user-id");

        var context = new ApplicationDbContext(options, currentUserService.Object);
        context.Database.EnsureCreated();
        return context;
    }

    private static UserManager<ApplicationUser> CreateUserManager(ApplicationDbContext context)
    {
        var store = new UserStore<ApplicationUser, IdentityRole, ApplicationDbContext, string>(context);
        var services = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();

        return new UserManager<ApplicationUser>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            new List<IUserValidator<ApplicationUser>> { new UserValidator<ApplicationUser>() },
            new List<IPasswordValidator<ApplicationUser>> { new PasswordValidator<ApplicationUser>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            services,
            services.GetRequiredService<ILogger<UserManager<ApplicationUser>>>());
    }

    private static RoleManager<IdentityRole> CreateRoleManager()
    {
        return new RoleManager<IdentityRole>(
            Mock.Of<IRoleStore<IdentityRole>>(),
            Array.Empty<IRoleValidator<IdentityRole>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Mock.Of<ILogger<RoleManager<IdentityRole>>>());
    }
}
