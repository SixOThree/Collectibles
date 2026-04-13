# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| Latest  | Yes       |

## Reporting a Vulnerability

If you discover a security vulnerability, please report it responsibly. **Do not open a public GitHub issue.**

### How to Report

1. Use [GitHub's private vulnerability reporting](https://github.com/SixOThree/Collectibles/security/advisories/new) to submit a report
2. Include a description of the vulnerability, steps to reproduce, and potential impact

### What to Expect

- **Acknowledgment** within 48 hours of your report
- **Status update** within 7 days with an assessment and timeline
- **Credit** in the fix announcement (unless you prefer to remain anonymous)

### Scope

This policy covers the Collectibles application code, its dependencies, and its default configuration. It does not cover vulnerabilities in third-party services (Azure, SQL Server, etc.) -- please report those to the respective vendors.

## Security Best Practices for Deployers

- Never use the default `HashIds:Salt` placeholder -- generate a unique value
- Always deploy with `ASPNETCORE_ENVIRONMENT=Production`
- Keep the .NET runtime and all NuGet packages up to date
- Review `Docs/Configuration.md` for all security-relevant settings
