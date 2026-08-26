using System.Security.Claims;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Constants;
using Collectibles.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Collectibles.Application.Tests.Infrastructure.Services;

public class CurrentUserServiceTests
{
    private static CurrentUserService CreateService(ClaimsPrincipal? user)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = user ?? new ClaimsPrincipal();

        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        return new CurrentUserService(accessorMock.Object);
    }

    [Fact]
    public void IsInRoleShouldReturnTrueWhenUserHasRole()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-1"),
                new Claim(ClaimTypes.Role, ApplicationConstants.Roles.UserManager),
            },
            "test"));

        var service = CreateService(user);

        service.IsInRole(ApplicationConstants.Roles.UserManager).Should().BeTrue();
        service.IsInRole(ApplicationConstants.Roles.Administrator).Should().BeFalse();
    }

    [Fact]
    public void IsInRoleShouldReturnFalseWhenNoHttpContext()
    {
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var service = new CurrentUserService(accessorMock.Object);

        service.IsInRole(ApplicationConstants.Roles.Administrator).Should().BeFalse();
    }
}
