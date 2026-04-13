using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration.Email;
using Collectibles.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Collectibles.Application.Features.EmailLogs.Commands;

public class RetryEmailCommand : IRequest
{
    public long EmailLogId { get; set; }
}

public class RetryEmailCommandHandler : IRequestHandler<RetryEmailCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<RetryEmailCommandHandler> _logger;
    private readonly IEventLogService _eventLogService;
    private readonly EmailSettings _emailSettings;

    public RetryEmailCommandHandler(
        IApplicationDbContext context,
        ILogger<RetryEmailCommandHandler> logger,
        IEventLogService eventLogService,
        IOptions<EmailSettings> emailSettings)
    {
        _context = context;
        _logger = logger;
        _eventLogService = eventLogService;
        _emailSettings = emailSettings.Value;
    }

    public async Task Handle(RetryEmailCommand request, CancellationToken cancellationToken)
    {
        var originalEmailLog = await _context.EmailLogs
            .FirstOrDefaultAsync(e => e.Id == request.EmailLogId, cancellationToken);

        if (originalEmailLog == null)
        {
            throw new InvalidOperationException($"Email log with ID {request.EmailLogId} not found.");
        }

        // Only allow resending emails that are not currently in progress or already pending
        if (originalEmailLog.Status == EmailStatus.InProgress || originalEmailLog.Status == EmailStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot resend email with status {originalEmailLog.Status}. Email is already queued or being sent.");
        }

        // Create a new email log record with the same information
        var newEmailLog = new EmailLog
        {
            ToEmail = originalEmailLog.ToEmail,
            ToName = originalEmailLog.ToName,
            CcEmails = originalEmailLog.CcEmails,
            BccEmails = originalEmailLog.BccEmails,
            FromEmail = _emailSettings.Sender.DefaultFromEmail,
            FromName = _emailSettings.Sender.DefaultFromName,
            Subject = originalEmailLog.Subject,
            Body = originalEmailLog.Body,
            IsHtml = originalEmailLog.IsHtml,
            Provider = originalEmailLog.Provider,
            TemplateName = originalEmailLog.TemplateName,
            TemplateData = originalEmailLog.TemplateData,
            Priority = originalEmailLog.Priority,
            Status = EmailStatus.Pending,
            AttemptCount = 0,
            LastAttemptAt = null,
            SentAt = null,
            ErrorMessage = null,
            MessageId = null,
            ScheduledFor = null,
        };

        _context.EmailLogs.Add(newEmailLog);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created new email log {NewEmailLogId} for resending original email {OriginalEmailLogId}",
            newEmailLog.Id,
            request.EmailLogId);

        await _eventLogService.LogEventAsync(
            EventAction.Other,
            "EmailLog",
            newEmailLog.Id,
            $"Email to {newEmailLog.ToEmail}",
            null,
            null,
            $"Email queued for resending (Subject: {newEmailLog.Subject}, Original ID: {request.EmailLogId})",
            cancellationToken);
    }
}
