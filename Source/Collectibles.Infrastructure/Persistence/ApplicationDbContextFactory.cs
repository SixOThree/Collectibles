using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Collectibles.Infrastructure.Persistence;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Get the environment from ASPNETCORE_ENVIRONMENT or default to Development
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        // Get the base path - try multiple approaches
        var basePath = GetConfigurationBasePath();

        Console.WriteLine($"ApplicationDbContextFactory - Environment: {environment}");
        Console.WriteLine($"ApplicationDbContextFactory - Base Path: {basePath}");
        Console.WriteLine($"ApplicationDbContextFactory - Current Directory: {Directory.GetCurrentDirectory()}");

        // Build configuration
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        Console.WriteLine($"ApplicationDbContextFactory - Connection String: {(!string.IsNullOrEmpty(connectionString) ? "Found" : "Not Found")}");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"Connection string 'DefaultConnection' not found. Environment: {environment}, BasePath: {basePath}");
        }

        optionsBuilder.UseSqlServer(connectionString);

        // Use the protected constructor that doesn't require ICurrentUserService
        return new ApplicationDbContext(optionsBuilder.Options);
    }

    private static string GetConfigurationBasePath()
    {
        // Try relative path first
        var relativePath = Path.Combine(Directory.GetCurrentDirectory(), "../Collectibles.Web");
        if (Directory.Exists(relativePath))
        {
            return Path.GetFullPath(relativePath);
        }

        // Try to find solution root and navigate from there
        var currentDir = Directory.GetCurrentDirectory();
        var searchDir = currentDir;

        // Walk up the directory tree to find the solution root
        while (searchDir != null && !File.Exists(Path.Combine(searchDir, "Collectibles.sln")))
        {
            searchDir = Directory.GetParent(searchDir)?.FullName;
            if (searchDir == Path.GetPathRoot(searchDir))
            {
                break;
            }
        }

        if (searchDir != null && File.Exists(Path.Combine(searchDir, "Collectibles.sln")))
        {
            var webPath = Path.Combine(searchDir, "Source", "Collectibles.Web");
            if (Directory.Exists(webPath))
            {
                return webPath;
            }
        }

        // As a last resort, try common paths where VS might run from
        var possiblePaths = new[]
        {
            @"C:\OneDrive\Development\Collectibles_Projects\Collectibles1\Source\Collectibles.Web",
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                @"OneDrive\Development\Collectibles_Projects\Collectibles1\Source\Collectibles.Web"),
        };

        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                return path;
            }
        }

        // Return the original relative path as fallback
        return relativePath;
    }
}
