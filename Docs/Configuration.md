# Configuration Reference

All application settings are in `Source/Collectibles.Web/appsettings.json`. Environment-specific overrides go in environment-specific `appsettings.{Environment}.json` (e.g., `appsettings.Development.json` or `appsettings.Production.json`).

---

## ConnectionStrings

Database connection string for SQL Server.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `DefaultConnection` | string | `""` | SQL Server connection string. Also used as the Hangfire connection if `Hangfire:ConnectionString` is empty. |

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CollectiblesDb;Trusted_Connection=True;"
  }
}
```

---

## SiteSettings

Branding displayed throughout the UI.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `SiteName` | string | `"Collectibles"` | Application name used in navigation and metadata. |
| `SiteTitle` | string | `"Collectibles"` | Browser tab / page title. |
| `SiteDescription` | string | `"Your ultimate platform..."` | Meta description for SEO and social sharing. |

```json
{
  "SiteSettings": {
    "SiteName": "Collectibles",
    "SiteTitle": "Collectibles",
    "SiteDescription": "Your ultimate platform for managing and showcasing your prized possessions."
  }
}
```

---

## Storage

Controls where file attachments are stored. Three providers are available:
- `Database`
- `LocalFileSystem`
- `AzureBlobStorage` (most well tested)

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Provider` | string | `"LocalFileSystem"` | Active storage provider. One of `Database`, `LocalFileSystem`, or `AzureBlobStorage`. |

### Storage:Database

No additional settings. File data is stored in the SQL Server database using the `DefaultConnection` connection string.

### Storage:LocalFileSystem

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `BasePath` | string | `"Uploads"` | Directory for uploaded files, relative to the web root unless `UseAbsolutePath` is true. |
| `UseAbsolutePath` | bool | `false` | When true, `BasePath` is treated as an absolute filesystem path. |

### Storage:AzureBlobStorage

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `ConnectionString` | string | `""` | Azure Storage account connection string. |
| `ContainerName` | string | `"collectibles-attachments"` | Blob container name. |
| `CreateContainerIfNotExists` | bool | `true` | Automatically create the container on startup. |
| `SubfolderPath` | string | `""` | Optional virtual directory prefix prepended to all blob names. When set (e.g., `"production/v1"`), a blob saved as `123/file.jpg` is stored as `production/v1/123/file.jpg`. This lets multiple applications or environments share the same container while keeping their files separated. Leading and trailing slashes are trimmed automatically. |

### Storage:DirectUpload

Enables browser-to-Azure direct uploads (bypassing the website) via SAS tokens for large files. Only applies when `Provider` is `AzureBlobStorage`. See [Large File Uploads](LargeFileUploads.md) for details.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Enabled` | bool | `true` | Enable direct upload support. |
| `ThresholdBytes` | long | `52428800` (50 MB) | Files at or above this size use direct upload instead of server-proxied upload. |
| `SasExpiryMinutes` | int | `30` | Lifetime of generated SAS tokens in minutes. |

```json
{
  "Storage": {
    "Provider": "LocalFileSystem",
    "LocalFileSystem": {
      "BasePath": "wwwroot/uploads",
      "UseAbsolutePath": false
    },
    "AzureBlobStorage": {
      "ConnectionString": "",
      "ContainerName": "collectibles-attachments",
      "CreateContainerIfNotExists": false,
      "SubfolderPath": ""
    },
    "DirectUpload": {
      "Enabled": true,
      "ThresholdBytes": 52428800,
      "SasExpiryMinutes": 30
    }
  }
}
```

---

## PreviewGeneration

Controls which attachment types have preview thumbnails generated. Disable categories to skip preview processing for specific file types (e.g., disable Video on servers without FFmpeg, or disable PDF where pdfium is unavailable). When a category is disabled, attachments of that type have no preview image.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Images` | bool | `true` | Generate thumbnail previews for image attachments (JPEG, PNG, GIF, WebP, BMP, TIFF). |
| `Pdf` | bool | `true` | Generate rendered first-page previews for PDF attachments. Requires pdfium native library. |
| `Video` | bool | `true` | Generate frame-capture previews for video attachments. Requires FFmpeg installed on the server. |
| `Word` | bool | `true` | Generate text-extract previews for Word documents (.doc, .docx). |
| `PowerPoint` | bool | `true` | Generate text-extract previews for PowerPoint documents (.ppt, .pptx). |

```json
{
  "PreviewGeneration": {
    "Images": true,
    "Pdf": true,
    "Video": true,
    "Word": true,
    "PowerPoint": true
  }
}
```

---

## HashIds

ID obfuscation settings. Database IDs are never exposed to users; HashIds encodes them into short, URL-safe strings at application boundaries. For example, a showcase with database ID `42` might appear as `/showcase/x9K2mNpQ` in the URL instead of `/showcase/42`. This prevents enumeration of resources and hides internal sequencing.

The salt determines the encoding — the same ID with different salts produces completely different hashes. **Changing the salt after deployment invalidates every existing public URL, shared link, and QR code.**

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Salt` | string | `"YOUR_UNIQUE_SALT_HERE"` | **Must be changed before first use.** Unique salt for encoding/decoding IDs. Use a long, random string (e.g., a generated UUID or passphrase). |
| `MinHashLength` | int | `8` | Minimum length of generated hash strings. Shorter values produce shorter URLs but are easier to guess. |
| `Alphabet` | string | `"abcdef...1234567890"` | Characters used in generated hashes. Default is alphanumeric (no special characters), which keeps URLs clean without encoding. |

```json
{
  "HashIds": {
    "Salt": "YOUR_UNIQUE_SALT_HERE",
    "MinHashLength": 8,
    "Alphabet": "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890"
  }
}
```

---

## QRCode

QR codes are meant to be printed and scanned by anyone, so the URL embedded in them must be publicly reachable. When `BaseUrl` is empty, the application uses the URL from the current browser request — which means QR codes generated while accessing the site at `http://localhost:5111` or a local IP like `https://192.168.1.5:7269` would contain those internal addresses. Anyone scanning the code outside your network would get a broken link.

Set `BaseUrl` to your public-facing URL to ensure QR codes always work regardless of how you access the admin interface.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `BaseUrl` | string | `""` | Base URL embedded in generated QR codes. When empty, the current request URL is used. Set this to your public domain (e.g., `"https://mycollectibles.com"`) to ensure QR codes work when scanned outside your local network. |

```json
{
  "QRCode": {
    "BaseUrl": ""
  }
}
```

---

## PasswordPolicy

Controls ASP.NET Identity password requirements and account lockout behavior.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `RequiredLength` | int | `12` | Minimum password length. |
| `RequireDigit` | bool | `true` | Require at least one digit. |
| `RequireLowercase` | bool | `true` | Require at least one lowercase letter. |
| `RequireUppercase` | bool | `true` | Require at least one uppercase letter. |
| `RequireNonAlphanumeric` | bool | `true` | Require at least one special character. |
| `RequiredUniqueChars` | int | `6` | Minimum number of distinct characters. |
| `MaximumLength` | int | `128` | Maximum password length. |
| `PasswordHistoryCount` | int | `5` | Number of previous passwords remembered to prevent reuse. |
| `LockoutMinutes` | int | `15` | Duration of account lockout after too many failed attempts. |
| `MaxFailedAttempts` | int | `5` | Failed login attempts before lockout triggers. |
| `LockoutOnFailure` | bool | `true` | Whether failed login attempts count toward account lockout. |

```json
{
  "PasswordPolicy": {
    "RequiredLength": 12,
    "RequireDigit": true,
    "RequireLowercase": true,
    "RequireUppercase": true,
    "RequireNonAlphanumeric": true,
    "RequiredUniqueChars": 6,
    "MaximumLength": 128,
    "PasswordHistoryCount": 5,
    "LockoutMinutes": 15,
    "MaxFailedAttempts": 5,
    "LockoutOnFailure": true
  }
}
```

---

## EmailSettings

Email delivery configuration. Four providers are available:
- `SMTP`
- `SendGrid`
- `AzureCommunication`
- `NULL` (log only, no emails sent)

See [Email Configuration](EmailConfiguration.md) for detailed setup.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Provider` | string | `"NULL"` | Active email provider. One of `SMTP`, `SendGrid`, `AzureCommunication`, or `NULL`. |
| `EnableEmailLogging` | bool | `true` | Log all email sends to the database for auditing. |
| `LogRetentionDays` | int | `365` | Number of days to keep sent email logs before automatic cleanup. Only sent emails are deleted; failed or pending emails are always kept. |

### EmailSettings:Smtp

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Host` | string | `"smtp.gmail.com"` | SMTP server hostname. |
| `Port` | int | `587` | SMTP server port (587 for STARTTLS, 465 for SSL). |
| `Username` | string | `""` | SMTP authentication username. |
| `Password` | string | `""` | SMTP authentication password. |
| `EnableSsl` | bool | `true` | Use TLS/SSL for the SMTP connection. |
| `TimeoutSeconds` | int | `30` | Connection and send timeout. |

### EmailSettings:SendGrid

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `ApiKey` | string | `""` | SendGrid API key. |
| `SandboxMode` | bool | `false` | When true, emails are validated but not delivered. |

### EmailSettings:AzureCommunication

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `ConnectionString` | string | `""` | Azure Communication Services connection string. |
| `SenderAddress` | string | `""` | Verified sender email address. |
| `PollingIntervalSeconds` | int | `1` | How often to poll for delivery status. |
| `TimeoutSeconds` | int | `120` | Maximum time to wait for delivery confirmation. |

### EmailSettings:Sender

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `DefaultFromEmail` | string | `""` | Default "From" email address. |
| `DefaultFromName` | string | `""` | Default "From" display name. |
| `ReplyToEmail` | string | `""` | Reply-to email address (optional). |
| `ReplyToName` | string | `""` | Reply-to display name (optional). |

### EmailSettings:Retry

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `MaxAttempts` | int | `3` | Maximum delivery retry attempts. |
| `InitialDelaySeconds` | int | `5` | Delay before the first retry. |
| `MaxDelaySeconds` | int | `300` | Maximum delay between retries (caps exponential backoff). |
| `BackoffMultiplier` | double | `2.0` | Multiplier for exponential backoff between retries. |

```json
{
  "EmailSettings": {
    "Provider": "NULL",
    "EnableEmailLogging": true,
    "LogRetentionDays": 365,
    "Smtp": {
      "Host": "smtp.gmail.com",
      "Port": 587,
      "Username": "",
      "Password": "",
      "EnableSsl": true,
      "TimeoutSeconds": 30
    },
    "SendGrid": {
      "ApiKey": "",
      "SandboxMode": false
    },
    "AzureCommunication": {
      "ConnectionString": "",
      "SenderAddress": "",
      "PollingIntervalSeconds": 1,
      "TimeoutSeconds": 120
    },
    "Sender": {
      "DefaultFromEmail": "noreply@collectibles.com",
      "DefaultFromName": "Collectibles",
      "ReplyToEmail": "",
      "ReplyToName": ""
    },
    "Retry": {
      "MaxAttempts": 3,
      "InitialDelaySeconds": 5,
      "MaxDelaySeconds": 300,
      "BackoffMultiplier": 2.0
    }
  }
}
```

---

## EntityFramework

EF Core diagnostic settings. These should generally stay disabled in production.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `EnableSensitiveDataLogging` | bool | `false` | Include parameter values in EF Core log messages. **Do not enable in production.** |
| `EnableDetailedErrors` | bool | `false` | Include detailed error information in EF exceptions. |
| `QueryExecutionWarningThresholdMilliseconds` | int | `100` | Log a warning when a query exceeds this duration. |

```json
{
  "EntityFramework": {
    "EnableSensitiveDataLogging": false,
    "EnableDetailedErrors": false,
    "QueryExecutionWarningThresholdMilliseconds": 100
  }
}
```

---

## Serilog

Structured logging configuration using Serilog. See [Serilog documentation](https://github.com/serilog/serilog-settings-configuration) for full options.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `MinimumLevel:Default` | string | `"Information"` | Default minimum log level. |
| `MinimumLevel:Override:{Source}` | string | varies | Per-namespace level overrides. |
| `EnableDatabaseLogging` | bool | `true` | Write log entries to the database in addition to file sinks. |

The default configuration writes to two rolling log files:
- `Logs/collectibles-{date}.txt` — all messages at Information and above
- `Logs/errors-{date}.txt` — errors only

Both retain 30 days of logs.

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning",
        "Hangfire": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "Logs/collectibles-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "Logs/errors-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "restrictedToMinimumLevel": "Error",
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"],
    "Properties": {
      "Application": "Collectibles"
    },
    "EnableDatabaseLogging": true
  }
}
```

---

## Logging

Controls retention for application logs stored in the database.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `RequestLogRetentionDays` | int | `365` | Number of days to keep HTTP request logs before automatic cleanup. Request logs capture method, path, status code, response time, user, and IP for every request. |

```json
{
  "Logging": {
    "RequestLogRetentionDays": 365
  }
}
```

---

## Hangfire

Background job processing with Hangfire.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `ConnectionString` | string | `""` | SQL Server connection string for Hangfire storage. Falls back to `ConnectionStrings:DefaultConnection` when empty. |
| `DashboardPath` | string | `"/hangfire"` | URL path for the Hangfire dashboard. |
| `RequireAuthorization` | bool | `true` | Require admin authentication to access the dashboard. Set to `false` only in development. |

### Built-in Recurring Jobs

These jobs are registered automatically. Schedules are not configurable via appsettings.

| Job ID | Schedule | Description |
|--------|----------|-------------|
| `process-pending-emails` | Every minute | Sends queued emails. |
| `cleanup-old-email-logs` | Daily 2:00 AM | Removes sent email logs older than `EmailSettings:LogRetentionDays` (default 365 days). Failed and pending emails are always kept. |
| `cleanup-old-request-logs` | Daily 3:00 AM | Removes request logs older than `Logging:RequestLogRetentionDays` (default 365 days). |
| `cleanup-orphaned-zip-upload-jobs` | Every hour | Cleans up stale zip upload jobs. |
| `process-unhashed-attachments` | Every 5 minutes | Generates content hashes for new attachments. |
| `generate-missing-attachment-previews` | Every 5 minutes | Creates preview images for attachments that lack them. |

```json
{
  "Hangfire": {
    "ConnectionString": "",
    "DashboardPath": "/hangfire",
    "RequireAuthorization": true
  }
}
```

---

## ApiKey

API key authentication for programmatic access.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Key` | string | `""` | The API key value. Leave empty to disable API key authentication. |
| `UserEmail` | string | `""` | Email of the user account that API requests execute as. |

```json
{
  "ApiKey": {
    "Key": "",
    "UserEmail": ""
  }
}
```

---

## ExternalLinks

Controls external link functionality on collectible items.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Enabled` | `bool` | `true` | When `false`, external links are completely hidden from the UI and cannot be added. Existing data is preserved. |
| `CachingEnabled` | `bool` | `true` | When `false`, the production link caching service (which uses Playwright to capture external URL screenshots and HTML snapshots) is disabled. Links display as simple URLs without snapshots. Only meaningful when `Enabled` is `true`. This is unrelated to the Playwright E2E test suite. |

```json
"ExternalLinks": {
    "Enabled": true,
    "CachingEnabled": true
}
```

---

## CrawlerBlocking

Blocks known web crawlers, scrapers, and bots based on User-Agent strings.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Enabled` | bool | `true` | Enable crawler blocking middleware. |
| `BlockEmptyUserAgent` | bool | `true` | Block requests with no User-Agent header. |

The middleware maintains hardcoded lists of blocked User-Agents (AI crawlers, SEO bots, vulnerability scanners) and allowed User-Agents (major browsers, Googlebot, Bingbot, etc.). These lists are defined in `CrawlerBlockingOptions.cs`.

```json
{
  "CrawlerBlocking": {
    "Enabled": true,
    "BlockEmptyUserAgent": true
  }
}
```

---

## SecurityScanBlocking

Blocks IP addresses that repeatedly probe for common vulnerability paths (WordPress, PHP, etc.).

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Enabled` | bool | `true` | Enable security scan blocking middleware. |
| `MaxAttemptsBeforeBlock` | int | `3` | Number of suspicious requests before blocking the IP. |
| `BlockDurationHours` | int | `24` | How long a blocked IP stays blocked. |
| `TrackingWindowMinutes` | int | `30` | Rolling window for counting suspicious requests. |
| `UseForwardedHeaders` | bool | `true` | Read client IP from `X-Forwarded-For` header (enable when behind a reverse proxy). |
| `SuspiciousPatterns` | string[] | *(see below)* | Regex patterns matched against request paths. |

Default suspicious patterns detect requests for WordPress, PHP files, phpMyAdmin, CMS platforms, CGI scripts, source control files, and shell commands.

```json
{
  "SecurityScanBlocking": {
    "Enabled": true,
    "MaxAttemptsBeforeBlock": 3,
    "BlockDurationHours": 24,
    "TrackingWindowMinutes": 30,
    "UseForwardedHeaders": true,
    "SuspiciousPatterns": [
      "wordpress|wp-admin|wp-content|wp-includes|wp-login",
      "\\.php$|\\.asp$|\\.aspx$|\\.jsp$",
      "phpmyadmin|pma|mysql|mysqladmin",
      "joomla|drupal|magento|prestashop",
      "cgi-bin|scripts|fckeditor|ckfinder",
      "\\.git|\\.svn|\\.env|\\.config",
      "shell|cmd|eval|exec|system"
    ]
  }
}
```

---

## AllowedHosts

A built-in ASP.NET Core security feature that validates the `Host` header on incoming HTTP requests. This protects against **host header injection attacks**, where an attacker sends a request with a forged `Host` header (e.g., `Host: evil.com`) to trick the application into generating links, redirects, or cache entries pointing to the attacker's domain.

When set to `"*"` (the default), any host header is accepted. In production, restrict it to your actual domain(s) so that requests with unexpected host headers are rejected with a 400 response before reaching your application code.

If you're behind a reverse proxy like Cloudflare, the proxy already validates the host header before the request reaches your server, so this acts as a defense-in-depth measure.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `AllowedHosts` | string | `"*"` | Semicolon-delimited list of allowed host headers. `*` allows all. |

```json
{
  "AllowedHosts": "mycollectibles.com;www.mycollectibles.com"
}
```

```json
{
  "AllowedHosts": "*"
}
```
