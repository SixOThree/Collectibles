using System.Security.Claims;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Constants;
using Collectibles.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Collectibles.Application.Tests.Infrastructure.Persistence;

public class ScopedApplicationDbContextFactoryTests
{
    private static ApplicationDbContext CreateContext(
        ClaimsPrincipal? user = null,
        Mock<IHttpContextDataService>? dataUserService = null)
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        if (dataUserService != null)
        {
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IHttpContextDataService)))
                .Returns(dataUserService.Object);
        }

        IHttpContextAccessor? accessor = null;
        if (user != null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = user;
            var accessorMock = new Mock<IHttpContextAccessor>();
            accessorMock.Setup(x => x.HttpContext).Returns(httpContext);
            accessor = accessorMock.Object;
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var factory = new ScopedApplicationDbContextFactory(serviceProviderMock.Object, options, accessor);
        return (ApplicationDbContext)factory.CreateDbContext();
    }

    private static ICurrentUserService GetCapturedCurrentUser(ApplicationDbContext context)
    {
        // ApplicationDbContext stores the injected ICurrentUserService in a private field.
        return (ICurrentUserService)typeof(ApplicationDbContext)
            .GetField("_currentUserService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(context)!;
    }

    private static ClaimsPrincipal CreateUser(params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "user-1") };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims.ToArray(), "test"));
    }

    [Fact]
    public void ShouldCaptureRolesFromHttpContext()
    {
        var context = CreateContext(CreateUser(ApplicationConstants.Roles.Administrator, ApplicationConstants.Roles.UserManager));

        var currentUser = GetCapturedCurrentUser(context);

        currentUser.IsAdministrator.Should().BeTrue();
        currentUser.IsInRole(ApplicationConstants.Roles.UserManager).Should().BeTrue();
        currentUser.IsInRole(ApplicationConstants.Roles.Viewer).Should().BeFalse();
    }

    [Fact]
    public void ShouldCaptureRolesFromHttpContextDataService()
    {
        var dataUserService = new Mock<IHttpContextDataService>();
        dataUserService.Setup(x => x.IsInitialized).Returns(true);
        dataUserService.Setup(x => x.IsAuthenticated).Returns(true);
        dataUserService.Setup(x => x.UserId).Returns("user-1");
        dataUserService.Setup(x => x.UserName).Returns("User One");
        dataUserService.Setup(x => x.UserRoles).Returns(new List<string> { ApplicationConstants.Roles.UserManager });

        var context = CreateContext(dataUserService: dataUserService);

        var currentUser = GetCapturedCurrentUser(context);

        currentUser.UserId.Should().Be("user-1");
        currentUser.IsAdministrator.Should().BeFalse();
        currentUser.IsInRole(ApplicationConstants.Roles.UserManager).Should().BeTrue();
    }

    [Fact]
    public void ShouldReportNoRolesWhenNoUserAvailable()
    {
        var context = CreateContext();

        var currentUser = GetCapturedCurrentUser(context);

        currentUser.UserId.Should().BeNull();
        currentUser.IsAdministrator.Should().BeFalse();
        currentUser.IsInRole(ApplicationConstants.Roles.Administrator).Should().BeFalse();
    }
}
