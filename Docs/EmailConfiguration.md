# Email Service Configuration Guide

## Overview

The Collectibles application supports multiple email service providers to handle transactional emails such as user registration confirmations, password resets, and notifications. The application uses a factory pattern to dynamically select the appropriate email service based on configuration.

## Supported Email Providers

The application supports four email service providers:

1. **SMTP** - Traditional SMTP email service (default)
2. **SendGrid** - Cloud-based email delivery service
3. **Azure Communication Services** - Microsoft's unified communication platform
4. **NULL** - Log-only service that doesn't send emails (for development/testing)

## Configuration Structure

All email settings are configured in the `appsettings.json` file under the `EmailSettings` section. The configuration follows this structure:

```json
{
  "EmailSettings": {
    "Provider": "SMTP",  // Options: "SMTP", "SENDGRID", "AZURECOMMUNICATION", "NULL"
    "EnableEmailLogging": true,
    "Smtp": { ... },
    "SendGrid": { ... },
    "AzureCommunication": { ... },
    "Sender": { ... },
    "Retry": { ... }
  }
}
```

## Provider-Specific Configuration

### 1. SMTP Configuration

SMTP is the default provider and works with any SMTP-compatible email server.

#### Gmail Configuration Example
```json
"EmailSettings": {
  "Provider": "SMTP",
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-app-specific-password",
    "EnableSsl": true,
    "TimeoutSeconds": 30
  }
}
```

> **Note for Gmail:** You must use an [App-Specific Password](https://support.google.com/accounts/answer/185833) instead of your regular Gmail password if 2FA is enabled.

#### Outlook/Office 365 Configuration Example
```json
"EmailSettings": {
  "Provider": "SMTP",
  "Smtp": {
    "Host": "smtp-mail.outlook.com",
    "Port": 587,
    "Username": "your-email@outlook.com",
    "Password": "your-password",
    "EnableSsl": true,
    "TimeoutSeconds": 30
  }
}
```

#### Corporate SMTP Server Example
```json
"EmailSettings": {
  "Provider": "SMTP",
  "Smtp": {
    "Host": "mail.yourcompany.com",
    "Port": 25,
    "Username": "noreply@yourcompany.com",
    "Password": "smtp-password",
    "EnableSsl": false,
    "TimeoutSeconds": 30
  }
}
```

#### SMTP Settings Explained
- **Host**: SMTP server address
- **Port**: Common ports are 25 (unencrypted), 465 (SSL), 587 (TLS)
- **Username**: Authentication username (usually the email address)
- **Password**: Authentication password
- **EnableSsl**: Enable SSL/TLS encryption
- **TimeoutSeconds**: Connection timeout in seconds

### 2. SendGrid Configuration

SendGrid is a cloud-based email service that provides reliable email delivery at scale.

#### Setup Steps
1. Create a [SendGrid account](https://sendgrid.com/)
2. Generate an API key from Settings → API Keys
3. Verify your sender domain or email address

#### Configuration Example
```json
"EmailSettings": {
  "Provider": "SENDGRID",
  "SendGrid": {
    "ApiKey": "SG.xxxxxxxxxxxxxxxxxxxxxx",
    "SandboxMode": false
  },
  "Sender": {
    "DefaultFromEmail": "noreply@yourdomain.com",
    "DefaultFromName": "Collectibles App"
  }
}
```

#### SendGrid Settings Explained
- **ApiKey**: Your SendGrid API key (keep this secret!)
- **SandboxMode**: When `true`, validates requests without sending emails (useful for testing)

### 3. Azure Communication Services Configuration

Azure Communication Services provides enterprise-grade email delivery with Azure integration.

#### Setup Steps
1. Create an Azure Communication Services resource in the [Azure Portal](https://portal.azure.com/)
2. Create an Email Communication Service resource
3. Configure a verified domain or use the Azure-provided domain
4. Get the connection string from the resource's Keys section

#### Configuration Example
```json
"EmailSettings": {
  "Provider": "AZURECOMMUNICATION",
  "AzureCommunication": {
    "ConnectionString": "endpoint=https://xxx.communication.azure.com/;accesskey=xxx",
    "SenderAddress": "DoNotReply@xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx.azurecomm.net",
    "PollingIntervalSeconds": 1,
    "TimeoutSeconds": 120
  }
}
```

#### Azure Communication Settings Explained
- **ConnectionString**: Azure Communication Services connection string
- **SenderAddress**: Verified sender email address (can be custom domain or Azure-provided)
- **PollingIntervalSeconds**: How often to check email send status
- **TimeoutSeconds**: Maximum time to wait for email delivery confirmation

### 4. NULL Email Service Configuration

The NULL email service is a special provider that doesn't send actual emails but logs them instead. This is perfect for development and testing environments.

#### Configuration Example
```json
"EmailSettings": {
  "Provider": "NULL",
  "EnableEmailLogging": true,
  "Sender": {
    "DefaultFromEmail": "noreply@test.collectibles.com",
    "DefaultFromName": "Collectibles Test"
  }
}
```

#### Features
- **No external dependencies**: Doesn't require SMTP server or API keys
- **Full logging**: Logs all email details to application logs
- **Database logging**: When `EnableEmailLogging` is true, emails are still logged to the database
- **Mock message IDs**: Generates fake message IDs for tracking
- **Safe for testing**: Perfect for development, staging, or demo environments

#### Use Cases
1. **Development**: Test email flows without sending real emails
2. **CI/CD Pipelines**: Run tests without email infrastructure
3. **Demo Environments**: Show email functionality without actual delivery
4. **Debugging**: Inspect email content in logs without delivery concerns

#### What Gets Logged
When using the NULL provider, the following information is logged:
- From address and name
- To, CC, and BCC recipients
- Subject line
- Body preview (first 500 characters)
- Attachment information
- Template processing details

Example log output:
```
[Information] NULL EMAIL SERVICE - Email NOT sent (logged only): 
From: Collectibles Test <noreply@test.collectibles.com>, 
To: John Doe <user@example.com>, 
Subject: Welcome to Collectibles
```

## Common Settings

### Sender Configuration

These settings apply to all providers:

```json
"Sender": {
  "DefaultFromEmail": "noreply@collectibles.com",
  "DefaultFromName": "Collectibles",
  "ReplyToEmail": "support@collectibles.com",
  "ReplyToName": "Collectibles Support"
}
```

- **DefaultFromEmail**: Default sender email address
- **DefaultFromName**: Display name for the sender
- **ReplyToEmail**: Email address for replies (optional)
- **ReplyToName**: Display name for reply-to address (optional)

### Retry Configuration

Automatic retry settings for failed email sends:

```json
"Retry": {
  "MaxAttempts": 3,
  "InitialDelaySeconds": 5,
  "MaxDelaySeconds": 300,
  "BackoffMultiplier": 2.0
}
```

- **MaxAttempts**: Maximum number of send attempts
- **InitialDelaySeconds**: Initial delay before first retry
- **MaxDelaySeconds**: Maximum delay between retries
- **BackoffMultiplier**: Exponential backoff multiplier (e.g., 2.0 means delays of 5s, 10s, 20s...)

### Email Logging

Enable or disable email activity logging:

```json
"EnableEmailLogging": true
```

When enabled, email send attempts and results are logged for debugging and auditing.

## Environment-Specific Configuration

### Development Environment

For development, you can use:
- **SMTP with Ethereal Email** (fake SMTP service)
- **SendGrid Sandbox Mode**
- **Local SMTP server** (e.g., Papercut, MailDev)

Example using Ethereal Email:
```json
"EmailSettings": {
  "Provider": "SMTP",
  "Smtp": {
    "Host": "smtp.ethereal.email",
    "Port": 587,
    "Username": "your-ethereal-username",
    "Password": "your-ethereal-password",
    "EnableSsl": true
  }
}
```

### Production Environment

For production, ensure:
1. Use secure API keys and passwords
2. Store sensitive values in environment variables or Azure Key Vault
3. Use verified sender domains
4. Configure proper SPF, DKIM, and DMARC records
5. Monitor email delivery metrics

## Using Environment Variables

Instead of storing sensitive values in `appsettings.json`, use environment variables:

```bash
# Windows (Command Prompt)
set EmailSettings__SendGrid__ApiKey=SG.xxxxxxxxxxxxxxxxxxxxxx

# Windows (PowerShell)
$env:EmailSettings__SendGrid__ApiKey="SG.xxxxxxxxxxxxxxxxxxxxxx"

# Linux/Mac
export EmailSettings__SendGrid__ApiKey="SG.xxxxxxxxxxxxxxxxxxxxxx"
```

## Testing Email Configuration

1. **Check Configuration on Startup**
   - The application logs the configured email provider on startup
   - Look for: `Email Provider: [PROVIDER_NAME]` in the logs

2. **Test Email Send**
   - Register a new user account to trigger a confirmation email
   - Use the "Forgot Password" feature to test password reset emails

3. **Monitor Logs**
   - Check application logs for email send attempts
   - Look for success/failure messages
   - Review retry attempts if emails fail

## Troubleshooting

### SMTP Issues

**Connection Timeout**
- Verify firewall rules allow outbound connections on the SMTP port
- Check if your ISP blocks common SMTP ports (25, 465, 587)
- Try different ports if available

**Authentication Failed**
- Verify username and password are correct
- For Gmail, ensure you're using an app-specific password
- Check if the account has 2FA enabled

**SSL/TLS Errors**
- Ensure `EnableSsl` matches your server's requirements
- Try different ports (465 for SSL, 587 for TLS)

### SendGrid Issues

**Invalid API Key**
- Verify the API key is correct and active
- Check API key permissions include "Mail Send"

**Sender Not Verified**
- Ensure sender email/domain is verified in SendGrid
- Check Single Sender Verification or Domain Authentication

### Azure Communication Services Issues

**Connection String Invalid**
- Verify the connection string format
- Ensure the resource is not deleted or disabled

**Sender Address Not Valid**
- Use a verified custom domain or Azure-provided address
- Format: `DoNotReply@[resource-id].azurecomm.net`

## Security Best Practices

1. **Never commit sensitive values** to source control
2. **Use environment variables** or secret management services
3. **Rotate API keys** regularly
4. **Monitor for unusual activity** in email logs
5. **Implement rate limiting** to prevent abuse
6. **Use verified sender domains** to improve deliverability
7. **Enable SPF, DKIM, and DMARC** for your domain

## Provider Comparison

| Feature | SMTP | SendGrid | Azure Communication | NULL |
|---------|------|----------|-------------------|------|
| Setup Complexity | Low | Medium | Medium | None |
| Cost | Free* | Pay-per-email | Pay-per-email | Free |
| Scalability | Limited | High | High | Unlimited |
| Delivery Analytics | No | Yes | Yes | N/A |
| Template Support | No | Yes | Limited | Yes |
| Webhook Support | No | Yes | Yes | N/A |
| Azure Integration | No | No | Yes | No |
| Actual Email Delivery | Yes | Yes | Yes | No |
| Good for Testing | No | Limited** | No | Yes |

*SMTP cost depends on your email server provider
**SendGrid Sandbox Mode can be used for testing without sending emails

## Additional Resources

- [SendGrid Documentation](https://docs.sendgrid.com/)
- [Azure Communication Services Email](https://learn.microsoft.com/en-us/azure/communication-services/concepts/email/email-overview)
- [Gmail SMTP Settings](https://support.google.com/mail/answer/7126229)
- [Outlook SMTP Settings](https://support.microsoft.com/en-us/office/pop-imap-and-smtp-settings-for-outlook-com-d088b986-291d-42b8-9564-9c414e2aa040)

## Support

If you encounter issues with email configuration:
1. Check the application logs for detailed error messages
2. Verify your configuration matches the examples above
3. Test with a different provider to isolate the issue
4. Ensure your network allows outbound connections to the email service