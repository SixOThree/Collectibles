using System.Runtime.Versioning;

using Microsoft.AspNetCore.DataProtection;

namespace Collectibles.Web.Extensions;

/// <summary>
/// Configures durable, named Data Protection key persistence.
/// </summary>
/// <remarks>
/// The Data Protection key ring protects Blazor component state, antiforgery tokens, and
/// the Identity authentication cookie. With no configuration at all — the previous state —
/// the keys land in the IIS app-pool user profile under the default provider: an app-pool
/// identity change, a profile unload on recycle, a server migration, or adding a second
/// node loses or fails to share the ring, which shows up as mass forced logouts and
/// antiforgery failures, and blocks scale-out entirely.
///
/// The key directory is configured via <c>DataProtection:KeyPath</c>; point it at a shared
/// path (or a UNC share) for multi-instance deployments. <c>SetApplicationName</c> is fixed
/// so the purpose string does not change with the content root.
/// </remarks>
public static class DataProtectionExtensions
{
    private const string DefaultApplicationName = "Collectibles";

    public static IServiceCollection AddDurableDataProtection(this IServiceCollection services, IConfiguration configuration)
    {
        var builder = services.AddDataProtection()
            .SetApplicationName(configuration["DataProtection:ApplicationName"] ?? DefaultApplicationName);

        var keyPath = configuration["DataProtection:KeyPath"];

        if (!string.IsNullOrWhiteSpace(keyPath))
        {
            var directory = new DirectoryInfo(keyPath);
            if (!directory.Exists)
            {
                directory.Create();
            }

            builder.PersistKeysToFileSystem(directory);

            if (OperatingSystem.IsWindows())
            {
                ProtectKeysWithDpapi(builder);
            }
        }

        return services;
    }

    [SupportedOSPlatform("windows")]
    private static void ProtectKeysWithDpapi(IDataProtectionBuilder builder)
        => builder.ProtectKeysWithDpapi();
}
