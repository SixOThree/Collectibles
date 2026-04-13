namespace Collectibles.Application.Tests.Common;

public abstract class QueryTestBase<TQuery, TResult> : BaseTestFixture
    where TQuery : IRequest<TResult>
{
    protected Mock<IRequestHandler<TQuery, TResult>> HandlerMock { get; }

    protected QueryTestBase()
    {
        HandlerMock = new Mock<IRequestHandler<TQuery, TResult>>();
    }

    protected async Task<TResult> Act(TQuery query)
    {
        var handler = CreateHandler();
        return await handler.Handle(query, CancellationToken);
    }

    protected abstract IRequestHandler<TQuery, TResult> CreateHandler();

    protected void SeedData(params BaseEntity[] entities)
    {
        Context.AddRange(entities);
        Context.SaveChanges();
    }

    protected void VerifyMocks()
    {
        Mock.VerifyAll(HandlerMock);
    }
}
