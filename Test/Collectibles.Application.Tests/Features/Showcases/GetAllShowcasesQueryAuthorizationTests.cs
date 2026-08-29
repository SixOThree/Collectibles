using Collectibles.Application.Features.Showcases;
using Collectibles.Application.Features.Showcases.Queries;
using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;
using Collectibles.Application.Tests.Common;

using Moq;

namespace Collectibles.Application.Tests.Features.Showcases;

public class GetAllShowcasesQueryAuthorizationTests : QueryTestBase<GetAllShowcasesQuery, List<ShowcaseCardDto>>
{
    private readonly Mock<IShowcaseMappingService> _mappingServiceMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

    public GetAllShowcasesQueryAuthorizationTests()
    {
        _mappingServiceMock.Setup(x => x.MapManyToCardDtoAsync(
                It.IsAny<IEnumerable<Domain.Entities.Showcase>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShowcaseCardDto>());
    }

    protected override IRequestHandler<GetAllShowcasesQuery, List<ShowcaseCardDto>> CreateHandler()
    {
        return new GetAllShowcasesQueryHandler(
            Context,
            _mappingServiceMock.Object,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task HandleShouldThrowWhenNotAdministrator()
    {
        _currentUserServiceMock.Setup(x => x.IsAdministrator).Returns(false);

        var act = async () => await Act(new GetAllShowcasesQuery());

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task HandleShouldSucceedWhenAdministrator()
    {
        _currentUserServiceMock.Setup(x => x.IsAdministrator).Returns(true);

        var result = await Act(new GetAllShowcasesQuery());

        result.Should().NotBeNull();
    }
}
