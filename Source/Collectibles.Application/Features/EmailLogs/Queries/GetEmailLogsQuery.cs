using Collectibles.Application.Common.Models;
using Collectibles.Application.Features.EmailLogs.Dtos;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.EmailLogs.Queries;

public class GetEmailLogsQuery : IRequest<PaginatedList<EmailLogDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public EmailStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchTerm { get; set; }
    public string? ToEmail { get; set; }
}

public class GetEmailLogsQueryHandler : IRequestHandler<GetEmailLogsQuery, PaginatedList<EmailLogDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetEmailLogsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedList<EmailLogDto>> Handle(GetEmailLogsQuery request, CancellationToken cancellationToken)
    {
        // Email logs expose every recipient address and message subject on the system.
        if (!_currentUserService.IsAdministrator)
        {
            throw new UnauthorizedAccessException("Only administrators can view email logs.");
        }

        var query = _context.EmailLogs.AsQueryable();

        if (request.Status.HasValue)
        {
            query = query.Where(e => e.Status == request.Status.Value);
        }

        if (request.StartDate.HasValue)
        {
            query = query.Where(e => e.Created >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(e => e.Created <= request.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(e =>
                e.Subject.Contains(request.SearchTerm) ||
                e.ToEmail.Contains(request.SearchTerm) ||
                (e.ErrorMessage != null && e.ErrorMessage.Contains(request.SearchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(request.ToEmail))
        {
            query = query.Where(e => e.ToEmail.Contains(request.ToEmail));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.Created)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new EmailLogDto
            {
                Id = e.Id,
                ToEmail = e.ToEmail,
                ToName = e.ToName,
                CcEmails = e.CcEmails,
                BccEmails = e.BccEmails,
                FromEmail = e.FromEmail,
                FromName = e.FromName,
                Subject = e.Subject,
                Status = e.Status,
                AttemptCount = e.AttemptCount,
                LastAttemptAt = e.LastAttemptAt,
                SentAt = e.SentAt,
                ErrorMessage = e.ErrorMessage,
                Provider = e.Provider,
                Priority = e.Priority,
                ScheduledFor = e.ScheduledFor,
                TemplateName = e.TemplateName,
                Created = e.Created,
                LastModified = e.LastModified,
            })
            .ToListAsync(cancellationToken);

        return new PaginatedList<EmailLogDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
