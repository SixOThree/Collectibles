namespace Collectibles.Application.Common.Models.Email;

public class TemplatedEmailMessage : EmailMessage
{
    public string TemplateName { get; set; } = string.Empty;
    public object TemplateModel { get; set; } = new { };
}
