using Collectibles.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.ContentDefinitions.Commands;

public class SetDefaultContentDefinitionCommandHandler : IRequestHandler<SetDefaultContentDefinitionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SetDefaultContentDefinitionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(SetDefaultContentDefinitionCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAdministrator)
        {
            throw new UnauthorizedAccessException("Only administrators can set the default template.");
        }

        var newDefault = await _context.ContentDefinitions
            .FirstOrDefaultAsync(cd => cd.Id == request.Id, cancellationToken);

        if (newDefault == null)
        {
            // Or handle as an error
            return;
        }

        var currentDefaults = await _context.ContentDefinitions
            .Where(cd => cd.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var currentDefault in currentDefaults)
        {
            currentDefault.IsDefault = false;
        }

        newDefault.IsDefault = true;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
