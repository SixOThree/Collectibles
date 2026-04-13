namespace Collectibles.Application.Interfaces;

public interface IEmailTemplateService
{
    Task<string> RenderTemplateAsync(string templateName, object model, CancellationToken cancellationToken = default);
    Task<bool> TemplateExistsAsync(string templateName, CancellationToken cancellationToken = default);
    Task<string> GetTemplateSubjectAsync(string templateName, object model, CancellationToken cancellationToken = default);
}
