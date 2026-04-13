using Collectibles.Application.Common.Models.Email;
using Collectibles.Application.Interfaces;
using Collectibles.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Collectibles.Infrastructure.Services;

public class AspNetIdentityEmailSender : IEmailSender<ApplicationUser>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<AspNetIdentityEmailSender> _logger;

    public AspNetIdentityEmailSender(IEmailService emailService, ILogger<AspNetIdentityEmailSender> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        var message = new TemplatedEmailMessage
        {
            ToEmail = email,
            ToName = user.UserName,
            TemplateName = "EmailConfirmation",
            TemplateModel = new { ConfirmLink = confirmationLink, Name = user.UserName },
        };

        var result = await _emailService.SendTemplatedEmailAsync(message);

        if (!result.IsSuccess)
        {
            _logger.LogError(
                "Failed to send confirmation email to {Email}: {Error}",
                email, result.ErrorMessage);
        }
    }

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var message = new TemplatedEmailMessage
        {
            ToEmail = email,
            ToName = user.UserName,
            TemplateName = "PasswordReset",
            TemplateModel = new { ResetLink = resetLink, Name = user.UserName },
        };

        var result = await _emailService.SendTemplatedEmailAsync(message);

        if (!result.IsSuccess)
        {
            _logger.LogError(
                "Failed to send password reset email to {Email}: {Error}",
                email, result.ErrorMessage);
        }
    }

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        var message = new EmailMessage
        {
            ToEmail = email,
            ToName = user.UserName,
            Subject = "Your Password Reset Code",
            Body = $@"
                <h2>Password Reset Code</h2>
                <p>Your password reset code is: <strong>{resetCode}</strong></p>
                <p>This code will expire in 15 minutes.</p>
                <p>If you didn't request this, please ignore this email.</p>",
            IsHtml = true,
        };

        var result = await _emailService.SendEmailAsync(message);

        if (!result.IsSuccess)
        {
            _logger.LogError(
                "Failed to send password reset code to {Email}: {Error}",
                email, result.ErrorMessage);
        }
    }
}
