using Collectibles.Application.Common.Models.Email;

namespace Collectibles.Application.Interfaces;

public interface IEmailService
{
    Task<EmailResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);
    Task<EmailResult> SendTemplatedEmailAsync(TemplatedEmailMessage message, CancellationToken cancellationToken = default);
    Task<List<EmailResult>> SendBulkEmailAsync(List<EmailMessage> messages, CancellationToken cancellationToken = default);
}
