using Collectibles.Application.Interfaces;
using Collectibles.Application.Showcases.Commands.GenerateShareLink;
using Collectibles.Application.Showcases.Commands.RevokeShareToken;
using Collectibles.Application.Showcases.Queries.GetShareTokens;
using Collectibles.Application.Tests.Common;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Interfaces;

using Moq;

namespace Collectibles.Application.Tests.Features.Showcases;

public class GenerateShareLinkCommandAuthorizationTests : CommandTestBase<GenerateShareLinkCommand, GenerateShareLinkDto>
{
    private readonly Mock<IShowcaseShareTokenRepository> _shareTokenRepositoryMock = new();
    private readonly Mock<IEventLogService> _eventLogServiceMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

    protected override IRequestHandler<GenerateShareLinkCommand, GenerateShareLinkDto> CreateHandler()
    {
        return new GenerateShareLinkCommandHandler(
            _shareTokenRepositoryMock.Object,
            _eventLogServiceMock.Object,
            Context,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task HandleShouldThrowWhenShowcaseNotFound()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns("user-1");
        _currentUserServiceMock.Setup(x => x.IsAdministrator).Returns(false);

        var act = async () => await Act(new GenerateShareLinkCommand { ShowcaseId = 999L });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task HandleShouldThrowWhenNotOwnerAndNotAdmin()
    {
        var showcase = new Showcase { Name = "Other Showcase", UserId = "other-user" };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        _currentUserServiceMock.Setup(x => x.UserId).Returns("user-1");
        _currentUserServiceMock.Setup(x => x.IsAdministrator).Returns(false);

        var act = async () => await Act(new GenerateShareLinkCommand { ShowcaseId = showcase.Id });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _shareTokenRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ShowcaseShareToken>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleShouldSucceedWhenOwner()
    {
        var showcase = new Showcase { Name = "My Showcase", UserId = "user-1" };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        _shareTokenRepositoryMock
            .Setup(x => x.TokenExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _currentUserServiceMock.Setup(x => x.UserId).Returns("user-1");
        _currentUserServiceMock.Setup(x => x.IsAdministrator).Returns(false);

        var result = await Act(new GenerateShareLinkCommand { ShowcaseId = showcase.Id });

        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task HandleShouldSucceedWhenAdministrator()
    {
        var showcase = new Showcase { Name = "Other Showcase", UserId = "other-user" };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        _shareTokenRepositoryMock
            .Setup(x => x.TokenExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _currentUserServiceMock.Setup(x => x.UserId).Returns("user-1");
        _currentUserServiceMock.Setup(x => x.IsAdministrator).Returns(true);

        var result = await Act(new GenerateShareLinkCommand { ShowcaseId = showcase.Id });

        result.Should().NotBeNull();
    }
}

public class RevokeShareTokenCommandAuthorizationTests : CommandTestBase<RevokeShareTokenCommand, bool>
{
    private readonly Mock<IShowcaseShareTokenRepository> _shareTokenRepositoryMock = new();
    private readonly Mock<IEventLogService> _eventLogServiceMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

    protected override IRequestHandler<RevokeShareTokenCommand, bool> CreateHandler()
    {
        return new RevokeShareTokenCommandHandler(
            _shareTokenRepositoryMock.Object,
            _eventLogServiceMock.Object,
            Context,
            _currentUserServiceMock.Object);
    }

    private async Task<ShowcaseShareToken> CreateTokenForShowcase(string showcaseUserId)
    {
        var showcase = new Showcase { Name = "Showcase", UserId = showcaseUserId };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        var token = new ShowcaseShareToken
        {
            ShowcaseId = showcase.Id,
            Token = "test-token",
            IsActive = true,
        };
        Context.ShowcaseShareTokens.Add(token);
        await Context.SaveChangesAsync();

        _shareTokenRepositoryMock
            .Setup(x => x.GetByIdAsync(token.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        return token;
    }

    [Fact]
    public async Task HandleShouldReturnFalseWhenTokenNotFound()
    {
        _shareTokenRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShowcaseShareToken?)null);

        var result = await Act(new RevokeShareTokenCommand { TokenId = 999L });

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleShouldThrowWhenNotOwnerAndNotAdmin()
    {
        var token = await CreateTokenForShowcase("other-user");

        _currentUserServiceMock.Setup(x => x.UserId).Returns("user-1");
        _currentUserServiceMock.Setup(x => x.IsAdministrator).Returns(false);

        var act = async () => await Act(new RevokeShareTokenCommand { TokenId = token.Id });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task HandleShouldSucceedWhenOwner()
    {
        var token = await CreateTokenForShowcase("user-1");

        _currentUserServiceMock.Setup(x => x.UserId).Returns("user-1");
        _currentUserServiceMock.Setup(x => x.IsAdministrator).Returns(false);

        var result = await Act(new RevokeShareTokenCommand { TokenId = token.Id });

        result.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }
}

public class GetShareTokensQueryAuthorizationTests : QueryTestBase<GetShareTokensQuery, List<ShareTokenDto>>
{
    private readonly Mock<IShowcaseShareTokenRepository> _shareTokenRepositoryMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

    protected override IRequestHandler<GetShareTokensQuery, List<ShareTokenDto>> CreateHandler()
    {
        return new GetShareTokensQueryHandler(
            _shareTokenRepositoryMock.Object,
            Context,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task HandleShouldThrowWhenNotOwnerAndNotAdmin()
    {
        var showcase = new Showcase { Name = "Other Showcase", UserId = "other-user" };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        _currentUserServiceMock.Setup(x => x.UserId).Returns("user-1");
        _currentUserServiceMock.Setup(x => x.IsAdministrator).Returns(false);

        var act = async () => await Act(new GetShareTokensQuery { ShowcaseId = showcase.Id });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task HandleShouldSucceedWhenOwner()
    {
        var showcase = new Showcase { Name = "My Showcase", UserId = "user-1" };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        _shareTokenRepositoryMock
            .Setup(x => x.GetByShowcaseIdAsync(showcase.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShowcaseShareToken>());

        _currentUserServiceMock.Setup(x => x.UserId).Returns("user-1");
        _currentUserServiceMock.Setup(x => x.IsAdministrator).Returns(false);

        var result = await Act(new GetShareTokensQuery { ShowcaseId = showcase.Id });

        result.Should().NotBeNull().And.BeEmpty();
    }
}
