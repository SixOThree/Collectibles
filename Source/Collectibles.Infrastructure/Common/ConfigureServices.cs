using Collectibles.Application.Configuration;
using Collectibles.Application.Interfaces;
using Collectibles.Application.Services;
using Collectibles.Domain.Configuration;
using Collectibles.Domain.Configuration.Email;
using Collectibles.Domain.Configuration.Storage;
using Collectibles.Domain.Constants;
using Collectibles.Domain.Interfaces;
using Collectibles.Domain.Repositories;
using Collectibles.Infrastructure.FileProcessing;
using Collectibles.Infrastructure.FileStorage;
using Collectibles.Infrastructure.Persistence;
using Collectibles.Infrastructure.Persistence.Seeders;
using Collectibles.Infrastructure.Repositories;
using Collectibles.Infrastructure.Services;
using Collectibles.Infrastructure.Services.Email;
using Collectibles.Infrastructure.Services.Logging;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using SendGrid;

namespace Collectibles.Infrastructure.Common;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // Register regular DbContext (scoped) for runtime and Identity
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlServerOptionsAction: sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(ApplicationConstants.Database.MaxRetryDelaySeconds),
                        errorNumbersToAdd: null);
                    sqlOptions.CommandTimeout(60);

                    // Use split query by default to prevent cartesian explosion with complex Include chains
                    // Queries can still opt-in to single query behavior with AsSingleQuery() if needed
                    sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                })
                .EnableSensitiveDataLogging(configuration.GetValue<bool>("EntityFramework:EnableSensitiveDataLogging"))
                .EnableDetailedErrors(configuration.GetValue<bool>("EntityFramework:EnableDetailedErrors"))
                .ConfigureWarnings(warnings =>
                {
                    var thresholdMs = configuration.GetValue<int>("EntityFramework:QueryExecutionWarningThresholdMilliseconds", 100);
                    if (thresholdMs > 0)
                    {
                        warnings.Log((Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuting, Microsoft.Extensions.Logging.LogLevel.Debug));
                    }
                }));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Register the factory with proper scoping
        services.AddScoped<IApplicationDbContextFactory>(provider =>
        {
            // Get the current scope's service provider
            var options = provider.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
            var httpContextAccessor = provider.GetService<IHttpContextAccessor>();

            // Create a factory that captures the current user at creation time
            return new ScopedApplicationDbContextFactory(provider, options, httpContextAccessor);
        });

        services.AddScoped<IFileProcessingService, FileProcessingService>();
        services.AddScoped<ICollectibleItemPreviewService, CollectibleItemPreviewService>();
        services.AddScoped<ICollectibleItemPreviewResolver, CollectibleItemPreviewResolver>();

        // Register ICurrentUserService with appropriate implementation based on context
        services.AddScoped<ICurrentUserService>(serviceProvider =>
        {
            // Try to get AuthenticationStateProvider for Blazor Server components
            var authStateProvider = serviceProvider.GetService<AuthenticationStateProvider>();
            var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();

            // If we have AuthenticationStateProvider, use the Blazor-specific service
            // This will work correctly in Interactive Server mode
            // Pass IHttpContextAccessor as fallback for API endpoints (non-Blazor HTTP requests)
            if (authStateProvider != null)
            {
                return new BlazorCurrentUserService(authStateProvider, httpContextAccessor);
            }

            // Fall back to HttpContextAccessor for non-Blazor contexts
            // (middleware, API controllers, background services)
            return new CurrentUserService(httpContextAccessor);
        });
        services.AddSingleton<IHashIdsService, HashIdsService>();
        services.AddScoped<PlaywrightScenarioSeeder>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IQRCodeRepository, QRCodeRepository>();
        services.AddScoped<IShowcaseShareTokenRepository, ShowcaseShareTokenRepository>();
        services.AddScoped<ISiteConfigurationRepository, SiteConfigurationRepository>();
        services.AddScoped<IQRCodeGeneratorService, QRCodeGeneratorService>();
        services.AddScoped<IQRCodeUrlService, QRCodeUrlService>();
        services.AddScoped<IThemeService, ThemeService>();

        // Configure QR Code settings
        services.Configure<QRCodeSettings>(configuration.GetSection("QRCode"));

        // Configure storage settings
        services.Configure<StorageSettings>(configuration.GetSection(StorageSettings.SectionName));

        // Configure preview generation settings
        services.Configure<PreviewGenerationSettings>(configuration.GetSection(PreviewGenerationSettings.SectionName));

        // Configure email settings
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.ConfigurationSection));

        // Configure site settings
        services.Configure<SiteSettings>(configuration.GetSection("SiteSettings"));

        // Configure external links settings
        services.Configure<ExternalLinksOptions>(configuration.GetSection(ExternalLinksOptions.SectionName));

        // Register storage factory and file storage
        services.AddSingleton<IFileStorageFactory, FileStorageFactory>();
        services.AddScoped<IFileStorage>(provider =>
            provider.GetRequiredService<IFileStorageFactory>().CreateFileStorage());

        // Add database connectivity service to ensure SQL Server is available before starting
        // This must run BEFORE the DatabaseInitializerService
        services.AddHostedService<DatabaseConnectivityService>();

        // Add database initializer as hosted service
        services.AddHostedService<DatabaseInitializerService>();

        // Add theme initialization service
        services.AddHostedService<ThemeInitializationService>();

        // Add link processor service
        services.AddHostedService<LinkProcessorService>();
        services.AddScoped<ILinkProcessorService, ScopedLinkProcessorService>();

        // Egress validation for user-supplied URLs the server fetches (SSRF guard)
        services.AddSingleton<IUrlEgressGuard, UrlEgressGuard>();

        // Background work queued from handlers (never Task.Run on scoped services)
        services.AddScoped<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();

        // Register email services
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<SmtpEmailService>();
        // One SendGrid client for the process, built on a pooled HttpClient. The service
        // itself stays scoped; only the transport is shared.
        services.AddHttpClient(nameof(SendGridEmailService));
        services.AddSingleton<ISendGridClient>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<EmailSettings>>().Value;
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            return new SendGridClient(
                httpClientFactory.CreateClient(nameof(SendGridEmailService)),
                settings.SendGrid?.ApiKey ?? string.Empty);
        });
        services.AddScoped<SendGridEmailService>();
        services.AddScoped<AzureCommunicationEmailService>();
        services.AddScoped<NullEmailService>();
        services.AddScoped<EmailServiceFactory>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<EmailBackgroundService>();
        services.AddScoped<IEmailConfigurationService, EmailConfigurationService>();

        // Register logging services
        services.AddScoped<ISessionTrackingService, SessionTrackingService>();
        services.AddScoped<IEventLogService, EventLogService>();
        services.AddScoped<ISysLogService, SysLogService>();
        services.AddScoped<IRequestLogService, RequestLogService>();

        // Register attachment hash and duplicate detection services
        services.AddScoped<IAttachmentHashService, AttachmentHashService>();
        services.AddScoped<IAttachmentDuplicateDetectionService, AttachmentDuplicateDetectionService>();
        services.AddScoped<AttachmentIndexingBackgroundService>();
        services.AddScoped<AttachmentPreviewBackgroundService>();
        services.AddScoped<AttachmentPurgeBackgroundService>();

        return services;
    }

    public static IServiceCollection AddHangfireSchemaInitializer(this IServiceCollection services)
        => services.AddSingleton<IHangfireSchemaInitializer, HangfireSchemaInitializer>();
}
