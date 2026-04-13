using MediatR;

namespace Collectibles.Application.Features.ContentDefinitions.Commands;

public class SetDefaultContentDefinitionCommand : IRequest
{
    public long Id { get; set; }
}
