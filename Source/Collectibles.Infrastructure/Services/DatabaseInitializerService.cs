using Collectibles.Application.Interfaces;
using Collectibles.Application.Setup;
using Collectibles.Domain.ValueObjects.Templates;
using Collectibles.Infrastructure.Persistence;
using Collectibles.Infrastructure.Persistence.Seeders;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Collectibles.Infrastructure.Services;

public class DatabaseInitializerService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializerService> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private static readonly string[] Properties = new[] { "Vintage Computer", "Media (Books, Magazines)" };

    public DatabaseInitializerService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseInitializerService> logger,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Database initialization service starting...");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var setupTokenService = scope.ServiceProvider.GetRequiredService<ISetupTokenService>();
        var playwrightScenarioSeeder = scope.ServiceProvider.GetRequiredService<PlaywrightScenarioSeeder>();

        try
        {
            if (ShouldResetPlaywrightDatabase())
            {
                _logger.LogInformation("Playwright environment detected. Resetting database before migrations.");
                await context.Database.EnsureDeletedAsync(cancellationToken);
                _logger.LogInformation("Playwright database reset completed successfully");
            }

            // Log database migration start
            _logger.LogInformation("Starting database migration");

            // Ensure database is migrated FIRST before any SysLog access
            await context.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Database migrations applied successfully");

            // Now that migrations are complete, we can safely get the SysLog service
            var sysLogService = scope.ServiceProvider.GetRequiredService<ISysLogService>();

            await sysLogService.LogInformationAsync("Database migrations completed successfully", "Database.Migration", cancellationToken: cancellationToken);

            // Create roles
            await CreateRolesAsync(roleManager, sysLogService);

            if (ShouldResetPlaywrightDatabase())
            {
                _logger.LogInformation("Seeding deterministic Playwright data");
                await playwrightScenarioSeeder.SeedAsync(cancellationToken);
                await setupTokenService.DeleteTokenAsync();
            }

            // Check if setup is required (no administrators exist)
            if (await setupTokenService.IsSetupRequiredAsync())
            {
                // Generate setup token for first-run configuration
                await setupTokenService.GenerateSetupTokenAsync();
                _logger.LogWarning("No administrators found. Setup token has been generated for initial configuration.");
                await sysLogService.LogWarningAsync("Initial setup required - no administrators found", "Security.Setup", new Dictionary<string, object> { ["SetupTokenGenerated"] = true }, cancellationToken);
            }
            else
            {
                _logger.LogInformation("Administrator accounts found. System is configured.");
            }

            // Seed vintage computer tags
            await SeedVintageComputerTagsAsync(context, sysLogService);

            // Seed template data
            await SeedTemplatesAsync(context, sysLogService);

            _logger.LogInformation("Database initialization completed successfully");
            await sysLogService.LogInformationAsync("Database initialization completed successfully", "Application.Startup", cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing the database");

            // Try to log to SysLog, but don't fail if we can't
            try
            {
                var sysLogService = scope.ServiceProvider.GetService<ISysLogService>();
                if (sysLogService != null)
                {
                    await sysLogService.LogCriticalAsync("Database initialization failed", ex, "Database.Migration", cancellationToken: cancellationToken);
                }
            }
            catch
            {
                // If we can't log to SysLog, that's okay - we already logged to file/console
            }

            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private bool ShouldResetPlaywrightDatabase()
    {
        return _environment.IsEnvironment("Playwright")
            && _configuration.GetValue<bool>("PlaywrightSeed:ResetDatabaseOnStartup");
    }

    private async Task CreateRolesAsync(RoleManager<IdentityRole> roleManager, ISysLogService sysLogService)
    {
        string[] roleNames = { "Administrator", "UserManager", "Viewer" };

        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                _logger.LogInformation("Created role: {RoleName}", roleName);
                await sysLogService.LogInformationAsync(
                    $"Created security role: {roleName}",
                    "Security.Roles",
                    new Dictionary<string, object> { ["RoleName"] = roleName });
            }
        }
    }

    private async Task SeedVintageComputerTagsAsync(ApplicationDbContext context, ISysLogService sysLogService)
    {
        try
        {
            _logger.LogInformation("Checking for existing tags...");

            // Check if tags already exist
            if (await context.Tags.AnyAsync())
            {
                _logger.LogInformation("Tags already exist in database, skipping seed");
                return;
            }

            _logger.LogInformation("Seeding vintage computer tags...");
            await VintageComputerTagSeeder.SeedTagsAsync(context);

            var tagCount = await context.Tags.CountAsync();
            _logger.LogInformation("Successfully seeded {Count} vintage computer tags", tagCount);

            await sysLogService.LogInformationAsync(
                $"Seeded {tagCount} vintage computer tags",
                "Application.Startup",
                new Dictionary<string, object>
                {
                    ["TagCount"] = tagCount,
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed vintage computer tags");
            await sysLogService.LogErrorAsync(
                "Failed to seed vintage computer tags",
                ex,
                "Application.Startup");

            // Don't throw - allow the application to continue even if tag seeding fails
        }
    }

    private async Task SeedTemplatesAsync(ApplicationDbContext context, ISysLogService sysLogService)
    {
        try
        {
            // Check if templates already exist
            var existingTemplates = await context.ContentDefinitions
                .Where(cd => cd.IsGlobal && cd.IsActive)
                .ToListAsync();

            if (existingTemplates.Count != 0)
            {
                _logger.LogInformation("Templates already exist in database, skipping seed");
                return;
            }

            _logger.LogInformation("Seeding template data...");

            // 1. Vintage Computer
            var vintageComputerTemplate = new ContentDefinition
            {
                Name = "Vintage Computer",
                IsActive = true,
                IsDefault = true,
                IsGlobal = true,
                BorderColor = "#285ea4",
                Icon = "bi-pc-display-horizontal",
            };

            vintageComputerTemplate.SetTemplateDefinition(new TemplateDefinition
            {
                Name = "Vintage Computer",
                Version = "1.0",
                Fields = new List<FieldDefinition>
                {
                    new FieldDefinition { Name = "Manufacturer", Label = "Manufacturer", FieldType = FieldType.Text, DisplayOrder = 0, ValidationRules = new FieldValidationRules() },
                    new FieldDefinition { Name = "Model", Label = "Model", FieldType = FieldType.Text, DisplayOrder = 1, ValidationRules = new FieldValidationRules() },
                    new FieldDefinition { Name = "Provenance", Label = "Provenance", FieldType = FieldType.MultilineText, DisplayOrder = 2, ValidationRules = new FieldValidationRules() },
                    new FieldDefinition { Name = "InflationAdjustedPrice", Label = "Inflation Adjusted Price", FieldType = FieldType.InflationAdjustedPrice, DisplayOrder = 3, ValidationRules = new FieldValidationRules() },
                    new FieldDefinition { Name = "SerialNumber", Label = "Serial Number", FieldType = FieldType.Text, DisplayOrder = 4, ValidationRules = new FieldValidationRules() },
                    new FieldDefinition
                    {
                        Name = "Condition", Label = "Condition", FieldType = FieldType.Dropdown, DisplayOrder = 5, ValidationRules = new FieldValidationRules(),
                        Options = new Dictionary<string, object>
                        {
                            ["dropdownOptions"] = new List<string> { "New In Box", "Mint", "Very Good", "Good / Fair", "Poor", "Parts / Scrap" },
                        },
                    },
                    new FieldDefinition
                    {
                        Name = "WorkingStatus", Label = "Working Status", FieldType = FieldType.Dropdown, DisplayOrder = 6, ValidationRules = new FieldValidationRules(),
                        Options = new Dictionary<string, object>
                        {
                            ["dropdownOptions"] = new List<string> { "Fully Functional", "Needs Work", "Parts / Scrap", "Unknown" },
                        },
                    },
                },
            });
            context.ContentDefinitions.Add(vintageComputerTemplate);

            // 2. Magazine Collection
            var magazineTemplate = new ContentDefinition
            {
                Name = "Magazine Collection",
                IsActive = true,
                IsGlobal = true,
                Icon = "bi-book",
            };

            magazineTemplate.SetTemplateDefinition(new TemplateDefinition
            {
                Name = "Magazine Collection",
                Version = "1.0",
                AllowMultipleEntries = true,
                Fields = new List<FieldDefinition>
                {
                    new FieldDefinition { Name = "VolumeNumber", Label = "Volume Number", FieldType = FieldType.Text, DisplayOrder = 0, ValidationRules = new FieldValidationRules() },
                    new FieldDefinition { Name = "IssueNumber", Label = "Issue Number", FieldType = FieldType.Text, DisplayOrder = 1, ValidationRules = new FieldValidationRules() },
                    new FieldDefinition { Name = "PublishYear", Label = "Publish Year", FieldType = FieldType.Text, DisplayOrder = 2, ValidationRules = new FieldValidationRules() },
                    new FieldDefinition
                    {
                        Name = "PublishMonth", Label = "Publish Month", FieldType = FieldType.Dropdown, DisplayOrder = 3, ValidationRules = new FieldValidationRules(),
                        Options = new Dictionary<string, object>
                        {
                            ["dropdownOptions"] = new List<string> { "None", "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" },
                        },
                    },
                },
            });
            context.ContentDefinitions.Add(magazineTemplate);

            // 3. Grouping
            var groupingTemplate = new ContentDefinition
            {
                Name = "Grouping",
                IsActive = true,
                IsGlobal = true,
                HideAttachments = true,
            };

            groupingTemplate.SetTemplateDefinition(new TemplateDefinition
            {
                Name = "Grouping",
                Version = "1.0",
                Fields = new List<FieldDefinition>(),
            });
            context.ContentDefinitions.Add(groupingTemplate);

            // 4. Book Collection
            var bookTemplate = new ContentDefinition
            {
                Name = "Book Collection",
                IsActive = true,
                IsGlobal = true,
                Icon = "bi-book",
            };

            bookTemplate.SetTemplateDefinition(new TemplateDefinition
            {
                Name = "Book Collection",
                Version = "1.0",
                AllowMultipleEntries = true,
                Fields = new List<FieldDefinition>
                {
                    new FieldDefinition { Name = "Title", Label = "Title", FieldType = FieldType.Text, DisplayOrder = 0, ValidationRules = new FieldValidationRules() },
                    new FieldDefinition { Name = "Author", Label = "Author", FieldType = FieldType.Text, DisplayOrder = 1, ValidationRules = new FieldValidationRules() },
                    new FieldDefinition { Name = "ISBN", Label = "ISBN", FieldType = FieldType.Text, DisplayOrder = 2, ValidationRules = new FieldValidationRules() },
                },
            });
            context.ContentDefinitions.Add(bookTemplate);

            // 5. Game Console Software
            var gameConsoleSoftwareTemplate = new ContentDefinition
            {
                Name = "Game Console Software",
                IsActive = true,
                IsGlobal = true,
                Icon = "bi-dpad",
            };

            gameConsoleSoftwareTemplate.SetTemplateDefinition(new TemplateDefinition
            {
                Name = "Game Console Software",
                Version = "1.0",
                AllowMultipleEntries = true,
                Fields = new List<FieldDefinition>
                {
                    new FieldDefinition { Name = "GameTitle", Label = "Game Title", FieldType = FieldType.Text, DisplayOrder = 0, ValidationRules = new FieldValidationRules() },
                    new FieldDefinition { Name = "CartridgeDisc", Label = "Cartridge / Disc", FieldType = FieldType.Boolean, DisplayOrder = 1, ValidationRules = new FieldValidationRules() },
                    new FieldDefinition { Name = "Box", Label = "Box", FieldType = FieldType.Boolean, DisplayOrder = 2, ValidationRules = new FieldValidationRules() },
                    new FieldDefinition { Name = "Manual", Label = "Manual", FieldType = FieldType.Boolean, DisplayOrder = 3, ValidationRules = new FieldValidationRules() },
                    new FieldDefinition { Name = "Extras", Label = "Extras", FieldType = FieldType.Boolean, DisplayOrder = 4, ValidationRules = new FieldValidationRules() },
                    new FieldDefinition { Name = "Notes", Label = "Note", FieldType = FieldType.Text, DisplayOrder = 5, ValidationRules = new FieldValidationRules() },
                },
            });
            context.ContentDefinitions.Add(gameConsoleSoftwareTemplate);

            // 6. Game Console
            var gameConsoleTemplate = new ContentDefinition
            {
                Name = "Game Console",
                IsActive = true,
                IsGlobal = true,
                Icon = "bi-controller",
            };

            gameConsoleTemplate.SetTemplateDefinition(new TemplateDefinition
            {
                Name = "Game Console",
                Version = "1.0",
                Fields = new List<FieldDefinition>
                {
                    new FieldDefinition { Name = "Manufacturer", Label = "Manufacturer", FieldType = FieldType.Text, DisplayOrder = 0, ValidationRules = new FieldValidationRules() },
                    new FieldDefinition { Name = "Model", Label = "Model", FieldType = FieldType.Text, DisplayOrder = 1, ValidationRules = new FieldValidationRules() },
                },
            });
            context.ContentDefinitions.Add(gameConsoleTemplate);

            // 7. Software Collection
            var softwareTemplate = new ContentDefinition
            {
                Name = "Software Collection",
                IsActive = true,
                IsGlobal = true,
                Icon = "bi-floppy",
            };

            softwareTemplate.SetTemplateDefinition(new TemplateDefinition
            {
                Name = "Software Collection",
                Version = "1.0",
                AllowMultipleEntries = true,
                Fields = new List<FieldDefinition>
                {
                    new FieldDefinition { Name = "Title", Label = "Title", FieldType = FieldType.Text, DisplayOrder = 0, ValidationRules = new FieldValidationRules() },
                    new FieldDefinition { Name = "Publisher", Label = "Publisher/Developer", FieldType = FieldType.Text, DisplayOrder = 1, ValidationRules = new FieldValidationRules() },
                    new FieldDefinition { Name = "Category", Label = "Category", FieldType = FieldType.Text, DisplayOrder = 2, ValidationRules = new FieldValidationRules() },
                    new FieldDefinition { Name = "Platform", Label = "Platform", FieldType = FieldType.Text, DisplayOrder = 3, ValidationRules = new FieldValidationRules() },
                    new FieldDefinition { Name = "Format", Label = "Format", FieldType = FieldType.Text, DisplayOrder = 4, ValidationRules = new FieldValidationRules() },
                    new FieldDefinition { Name = "Notes", Label = "Notes", FieldType = FieldType.Text, DisplayOrder = 5, ValidationRules = new FieldValidationRules() },
                },
            });
            context.ContentDefinitions.Add(softwareTemplate);

            await context.SaveChangesAsync();

            const int templateCount = 7;
            _logger.LogInformation("Successfully seeded {Count} templates", templateCount);
            await sysLogService.LogInformationAsync(
                "Seeded default content templates",
                "Application.Startup",
                new Dictionary<string, object>
                {
                    ["Count"] = templateCount,
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed template data");
            await sysLogService.LogErrorAsync(
                "Failed to seed template data",
                ex,
                "Application.Startup");

            // Don't throw - allow the application to continue even if template seeding fails
        }
    }
}
