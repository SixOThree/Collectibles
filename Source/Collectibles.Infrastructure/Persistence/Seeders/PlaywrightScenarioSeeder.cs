using System.Text.Json;

using Collectibles.Application.Services;
using Collectibles.Domain.Entities;
using Collectibles.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Collectibles.Infrastructure.Persistence.Seeders;

public sealed class PlaywrightScenarioSeeder
{
    private const string DefaultPassword = "xA&%4hTVhTDixSOO";

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHashIdsService _hashIdsService;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public PlaywrightScenarioSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IHashIdsService hashIdsService,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _hashIdsService = hashIdsService;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await ResetStorageAsync(cancellationToken);

        var admin = await CreateUserAsync("test.admin@collectibles.local", "Playwright Admin", new[] { "Administrator" });
        var regular = await CreateUserAsync("test.user@collectibles.local", "Playwright User", Array.Empty<string>());
        var otherOwner = await CreateUserAsync("test.owner@collectibles.local", "Other Private Owner", Array.Empty<string>());

        var regularPrivate = new Showcase
        {
            Name = "PW Seed Private Showcase",
            Description = "Private showcase owned by the regular user.",
            IsPrivate = true,
            UserId = regular.Id
        };

        var regularPublic = new Showcase
        {
            Name = "PW Seed Public Showcase",
            Description = "Public showcase visible on the browse page.",
            IsPrivate = false,
            UserId = regular.Id
        };

        var otherPrivate = new Showcase
        {
            Name = "PW Seed Other User Private Showcase",
            Description = "Private showcase owned by another user.",
            IsPrivate = true,
            UserId = otherOwner.Id
        };

        _context.Showcases.AddRange(regularPrivate, regularPublic, otherPrivate);
        await _context.SaveChangesAsync(cancellationToken);

        var regularRoot = new CollectibleItem
        {
            Name = "PW Seed Root Item",
            DetailedDescription = "Root seeded item used by the item tests."
        };
        regularRoot.Showcases.Add(regularPrivate);

        var regularChild = new CollectibleItem
        {
            Name = "PW Seed Child Item",
            DetailedDescription = "Child seeded item used for breadcrumb and parent checks.",
            Parent = regularRoot
        };
        regularChild.Showcases.Add(regularPrivate);

        var otherPrivateItem = new CollectibleItem
        {
            Name = "PW Seed Other User Private Item",
            DetailedDescription = "Private item owned by another seeded user."
        };
        otherPrivateItem.Showcases.Add(otherPrivate);

        _context.CollectibleItems.AddRange(regularRoot, regularChild, otherPrivateItem);
        await _context.SaveChangesAsync(cancellationToken);

        var manifest = new PlaywrightSeedManifest
        {
            Users = new SeedUsers
            {
                Admin = new SeedUser { Email = admin.Email!, Password = DefaultPassword, DisplayName = admin.DisplayName ?? admin.Email! },
                Regular = new SeedUser { Email = regular.Email!, Password = DefaultPassword, DisplayName = regular.DisplayName ?? regular.Email! },
                OtherOwner = new SeedUser { Email = otherOwner.Email!, Password = DefaultPassword, DisplayName = otherOwner.DisplayName ?? otherOwner.Email! }
            },
            Showcases = new SeedShowcases
            {
                RegularPrivate = new SeedReference { Name = regularPrivate.Name, Hash = _hashIdsService.Encode(regularPrivate.Id) },
                RegularPublic = new SeedReference { Name = regularPublic.Name, Hash = _hashIdsService.Encode(regularPublic.Id) },
                OtherPrivate = new SeedReference { Name = otherPrivate.Name, Hash = _hashIdsService.Encode(otherPrivate.Id) }
            },
            Items = new SeedItems
            {
                RegularRoot = new SeedReference { Name = regularRoot.Name!, Hash = _hashIdsService.Encode(regularRoot.Id) },
                RegularChild = new SeedReference { Name = regularChild.Name!, Hash = _hashIdsService.Encode(regularChild.Id) },
                OtherPrivate = new SeedReference { Name = otherPrivateItem.Name!, Hash = _hashIdsService.Encode(otherPrivateItem.Id) }
            }
        };

        var manifestPath = _configuration["PlaywrightSeed:SeedManifestPath"] ?? "App_Data/playwright/seed-manifest.json";
        var fullManifestPath = Path.Combine(_environment.ContentRootPath, manifestPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullManifestPath)!);
        await File.WriteAllTextAsync(
            fullManifestPath,
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true }),
            cancellationToken);
    }

    private async Task<ApplicationUser> CreateUserAsync(string email, string displayName, IEnumerable<string> roles)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            FirstName = displayName.Split(' ')[0],
            LastName = displayName.Split(' ').Last(),
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, DefaultPassword);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(error => error.Description)));
        }

        foreach (var role in roles)
        {
            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(error => error.Description)));
            }
        }

        return user;
    }

    private Task ResetStorageAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.GetValue<bool>("PlaywrightSeed:ResetStorageOnStartup"))
        {
            return Task.CompletedTask;
        }

        var storageBasePath = _configuration["Storage:LocalFileSystem:BasePath"] ?? "App_Data/playwright/uploads";
        var fullStoragePath = Path.Combine(_environment.ContentRootPath, storageBasePath);
        if (Directory.Exists(fullStoragePath))
        {
            Directory.Delete(fullStoragePath, recursive: true);
        }

        var playwrightDataPath = Path.Combine(_environment.ContentRootPath, "App_Data", "playwright");
        if (Directory.Exists(playwrightDataPath))
        {
            Directory.Delete(playwrightDataPath, recursive: true);
        }

        var setupTokenPath = Path.Combine(_environment.ContentRootPath, "App_Data", "setup-token.txt");
        if (File.Exists(setupTokenPath))
        {
            File.Delete(setupTokenPath);
        }

        return Task.CompletedTask;
    }
}
