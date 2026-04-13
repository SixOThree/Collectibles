# Playwright Browser Setup for Production (IIS)

> **This document covers the production use of Playwright for external link caching** — a C# background service (`ScopedLinkProcessorService`) that uses the `Microsoft.Playwright` NuGet package to capture screenshots and HTML snapshots of external URLs added to collectible items. This is separate from the Node.js Playwright E2E test suite in `Test/Playwright/`, which is used only for development testing. See [Developer README](/Docs/DEVELOPER_README.md#e2e-tests-playwright--development-testing) for the testing use case.

## Recommended Approach: Explicit Browser Path via Environment Variable

The most reliable approach for service accounts is to explicitly set where Playwright should look for browsers.

### Configure via IIS Manager

1. In IIS Manager, select your site → **Configuration Editor**
2. Navigate to `system.webServer/aspNetCore` → **environmentVariables**
3. Add a new environment variable:
   - Name: `PLAYWRIGHT_BROWSERS_PATH`
   - Value: `C:\ProgramData\playwright-browsers` (or any shared location)
4. Install browsers to that location:

```powershell
$env:PLAYWRIGHT_BROWSERS_PATH = "C:\ProgramData\playwright-browsers"
cd "C:\path\to\your\published\app"
.\playwright.ps1 install chromium
```

5. Restart your application pool

This way, it does not matter what user account IIS uses — it will always look in the specified location.

## Installing to the System Profile

Run PowerShell as Administrator and execute these commands to install Playwright browsers for the system profile:

```powershell
# Set the environment variable to install browsers to the system profile location
$env:PLAYWRIGHT_BROWSERS_PATH = "C:\Windows\system32\config\systemprofile\AppData\Local\ms-playwright"

# Navigate to your application's published directory
cd "C:\path\to\your\published\app"

# Run the Playwright installation script
.\playwright.ps1 install chromium
```

Alternatively, install just the headless shell (which is what the app looks for):

```powershell
$env:PLAYWRIGHT_BROWSERS_PATH = "C:\Windows\system32\config\systemprofile\AppData\Local\ms-playwright"
.\playwright.ps1 install chromium-headless-shell
```

## Alternative: Change Application Pool Identity

If you prefer not to install browsers in the system profile location, change the IIS application pool to run under a specific user account:

1. Open IIS Manager
2. Go to **Application Pools**
3. Find your application pool and click **Advanced Settings**
4. Under **Identity**, change from `ApplicationPoolIdentity` to a specific user account that has the browsers installed
5. Restart the application pool

## Setting the Browser Path in Application Configuration

For a production environment, the cleanest approach is to set `PLAYWRIGHT_BROWSERS_PATH` in your application's configuration.

In `appsettings.Production.json` or as an IIS environment variable:

```json
{
  "PLAYWRIGHT_BROWSERS_PATH": "C:\\ProgramData\\playwright-browsers"
}
```

Then install browsers to that shared location:

```powershell
$env:PLAYWRIGHT_BROWSERS_PATH = "C:\ProgramData\playwright-browsers"
.\playwright.ps1 install chromium
```

This way, all application pools can share the same browser installation.
