namespace Collectibles.Application.Tests.Common;

public abstract class CommandTestBase<TCommand, TResult> : BaseTestFixture
    where TCommand : IRequest<TResult>
{
    protected Mock<IRequestHandler<TCommand, TResult>> HandlerMock { get; }

    protected CommandTestBase()
    {
        HandlerMock = new Mock<IRequestHandler<TCommand, TResult>>();
    }

    protected async Task<TResult> Act(TCommand command)
    {
        var handler = CreateHandler();
        return await handler.Handle(command, CancellationToken);
    }

    protected abstract IRequestHandler<TCommand, TResult> CreateHandler();

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
