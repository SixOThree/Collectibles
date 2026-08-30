using System.Threading.RateLimiting;

using Collectibles.Application.Common;
using Collectibles.Application.Interfaces;
using Collectibles.Application.Setup;
using Collectibles.Domain.Configuration;
using Collectibles.Domain.Constants;
using Collectibles.Domain.Interfaces;
using Collectibles.Infrastructure.Common;
using Collectibles.Infrastructure.Persistence;
using Collectibles.Infrastructure.Services;
using Collectibles.Web.Authentication;
using Collectibles.Web.Components.Account;
using Collectibles.Web.Middleware;
using Collectibles.Web.Services;

using Hangfire;
using Hangfire.SqlServer;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Web.Extensions;

public static class ServiceCollectionExtensions
{
    private static readonly string[] Tags = ["database", "sql"];

    /// <summary>
    /// Adds all web-related services including Blazor, SignalR, and form configuration.
    /// </summary>
    /// <returns></returns>
    public static IServiceCollection AddWebServices(this IServiceCollection services)
    {
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        // Add Razor Components with Interactive Server mode
        services.AddRazorComponents()
            .AddInteractiveServerComponents(options =>
            {
                // Configure circuit options for better performance
                options.JSInteropDefaultCallTimeout = TimeSpan.FromSeconds(ApplicationConstants.Web.JSInteropTimeoutSeconds);
                options.MaxBufferedUnacknowledgedRenderBatches = 10;
                options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(ApplicationConstants.Web.CircuitRetentionMinutes);
                options.DisconnectedCircuitMaxRetained = ApplicationConstants.RequestLimits.MaxDisconnectedCircuits;
            })
            .AddHubOptions(options =>
            {
                // Configure SignalR with reasonable message size limits
                options.MaximumReceiveMessageSize = ApplicationConstants.Web.SignalRMaxMessageSizeBytes; // 32 MB
                options.StreamBufferCapacity = ApplicationConstants.Web.SignalRStreamBufferCapacity;
                options.ClientTimeoutInterval = TimeSpan.FromMinutes(ApplicationConstants.Web.SignalRClientTimeoutMinutes);
                options.KeepAliveInterval = TimeSpan.FromSeconds(ApplicationConstants.Web.SignalRKeepAliveSeconds);
                options.HandshakeTimeout = TimeSpan.FromSeconds(ApplicationConstants.Web.SignalRHandshakeSeconds);

                // Every interactive component event runs over this hub, so detailed errors would
                // return exception type, message, and stack detail to the browser. Development only.
                options.EnableDetailedErrors = isDevelopment;
            });

        // Add Blazor Bootstrap
        services.AddBlazorBootstrap();

        // Configure form options for large file uploads
        services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = ApplicationConstants.RequestLimits.MaxRequestBodySize;
            options.ValueLengthLimit = int.MaxValue;
            options.MultipartHeadersLengthLimit = int.MaxValue;
            options.MultipartBoundaryLengthLimit = int.MaxValue;
        });

        // Add HttpContextAccessor for CurrentUserService and HttpContextDataService
        services.AddHttpContextAccessor();

        // Add HttpClient factory for components that need to make HTTP requests
        services.AddHttpClient();

        // Add HttpContextDataService for capturing HTTP context in Interactive Server components
        services.AddScoped<IHttpContextDataService, HttpContextDataService>();
        services.AddScoped<HttpContextDataService>();

        // Add memory caching for attachment previews
        services.AddMemoryCache();

        // Add health checks for monitoring application status
        services.AddHealthChecks()
            .AddSqlServer(
                connectionStringFactory: sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."),
                healthQuery: "SELECT 1",
                name: "database",
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
                tags: Tags,
                timeout: TimeSpan.FromSeconds(ApplicationConstants.Database.ConnectionTimeoutSeconds));

        // Add MiniProfiler for performance profiling (Development only)
        if (isDevelopment)
        {
            services.AddMiniProfiler(options =>
            {
                // Profiler URL route
                options.RouteBasePath = ApplicationConstants.ApiRoutes.MiniProfilerRouteBase;

                // Show SQL queries and parameters
                options.EnableServerTimingHeader = true;

                // Track connections and commands
                options.TrackConnectionOpenClose = true;

                // Color scheme
                options.ColorScheme = StackExchange.Profiling.ColorScheme.Auto;

                // SQL formatter
                options.SqlFormatter = new StackExchange.Profiling.SqlFormatters.InlineFormatter();

                // Ignore static files
                options.IgnoredPaths.Add(ApplicationConstants.ProfilerIgnorePaths.Css);
                options.IgnoredPaths.Add(ApplicationConstants.ProfilerIgnorePaths.Js);
                options.IgnoredPaths.Add(ApplicationConstants.ProfilerIgnorePaths.Lib);
                options.IgnoredPaths.Add(ApplicationConstants.ProfilerIgnorePaths.Themes);
                options.IgnoredPaths.Add(ApplicationConstants.ProfilerIgnorePaths.AppCss);
                options.IgnoredPaths.Add(ApplicationConstants.ProfilerIgnorePaths.Framework);
                options.IgnoredPaths.Add(ApplicationConstants.ProfilerIgnorePaths.Blazor);
            }).AddEntityFramework();
        }

        return services;
    }

    /// <summary>
    /// Configures authentication and authorization services.
    /// </summary>
    /// <returns></returns>
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCascadingAuthenticationState();
        services.AddScoped<IdentityUserAccessor>();
        services.AddScoped<IdentityRedirectManager>();
        services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        });

        authBuilder.AddIdentityCookies();

        // Pin the cookie's CSRF-relevant attributes rather than inheriting framework
        // defaults. Several state-changing API routes disable antiforgery yet accept this
        // cookie, so SameSite is load-bearing and must not be implicit.
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.HttpOnly = true;
        });

        authBuilder.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationHandler.SchemeName, null);

        // Add authorization with policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAuthenticated", policy => policy.RequireAuthenticatedUser());

            // Policy that accepts either API key or cookie authentication
            options.AddPolicy("ApiKeyOrCookie", policy =>
            {
                policy.AddAuthenticationSchemes(ApiKeyAuthenticationHandler.SchemeName, IdentityConstants.ApplicationScheme);
                policy.RequireAuthenticatedUser();
            });

            // The sync API is only ever called by the desktop SyncTool, which authenticates
            // with an API key. Restricting it to that scheme means an ambient browser
            // session cannot reach these antiforgery-disabled, state-changing routes at
            // all - the CSRF exposure is removed rather than left resting on the cookie's
            // SameSite default.
            options.AddPolicy("ApiKeyOnly", policy =>
            {
                policy.AddAuthenticationSchemes(ApiKeyAuthenticationHandler.SchemeName);
                policy.RequireAuthenticatedUser();
            });

            // User management policies
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(ApplicationConstants.Roles.Administrator));
            options.AddPolicy("UserManager", policy => policy.RequireRole(ApplicationConstants.Roles.Administrator, ApplicationConstants.Roles.UserManager));
            options.AddPolicy("CanViewUsers", policy => policy.RequireRole(ApplicationConstants.Roles.Administrator, ApplicationConstants.Roles.UserManager));
        });

        return services;
    }

    /// <summary>
    /// Configures ASP.NET Identity services.
    /// </summary>
    /// <returns></returns>
    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabaseDeveloperPageExceptionFilter();

        // Get password policy configuration
        var passwordPolicy = configuration.GetSection("PasswordPolicy");

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            // Sign-in requirements
            options.SignIn.RequireConfirmedAccount = true;

            // Password requirements - Strong defaults with config override
            options.Password.RequiredLength = passwordPolicy.GetValue("RequiredLength", 12);
            options.Password.RequireDigit = passwordPolicy.GetValue("RequireDigit", true);
            options.Password.RequireLowercase = passwordPolicy.GetValue("RequireLowercase", true);
            options.Password.RequireUppercase = passwordPolicy.GetValue("RequireUppercase", true);
            options.Password.RequireNonAlphanumeric = passwordPolicy.GetValue("RequireNonAlphanumeric", true);
            options.Password.RequiredUniqueChars = passwordPolicy.GetValue("RequiredUniqueChars", 6);

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(passwordPolicy.GetValue("LockoutMinutes", ApplicationConstants.Identity.DefaultLockoutMinutes));
            options.Lockout.MaxFailedAccessAttempts = passwordPolicy.GetValue("MaxFailedAttempts", ApplicationConstants.PasswordValidation.MaxFailedAccessAttempts);
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddSignInManager<CustomSignInManager>()
        .AddClaimsPrincipalFactory<CustomUserClaimsPrincipalFactory>()
        .AddDefaultTokenProviders()
        .AddPasswordValidator<CustomPasswordValidator>();

        // Configure token lifespans
        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromHours(ApplicationConstants.Identity.PasswordResetTokenHours); // Password reset tokens
        });

        // Configure ASP.NET Identity email sender to use our email service
        services.AddScoped<IEmailSender<ApplicationUser>, AspNetIdentityEmailSender>();

        // Register password history service
        services.AddScoped<IPasswordHistoryService, PasswordHistoryService>();

        return services;
    }

    /// <summary>
    /// Configures Hangfire background job processing services.
    /// </summary>
    /// <returns></returns>
    public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        var defaultConnectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(defaultConnectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found or is empty. Please configure it in appsettings.json or user secrets.");
        }

        // Use Hangfire-specific connection string if provided, otherwise use the default
        var hangfireConnectionString = configuration["Hangfire:ConnectionString"];
        if (string.IsNullOrWhiteSpace(hangfireConnectionString))
        {
            hangfireConnectionString = defaultConnectionString;
        }

        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(hangfireConnectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(ApplicationConstants.Database.CommandBatchMaxTimeoutMinutes),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(ApplicationConstants.Database.SlidingInvisibilityTimeoutMinutes),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true,

                // Disable automatic schema installation to prevent errors during startup
                PrepareSchemaIfNecessary = false,
            }));

        // Configure Hangfire server options with better error handling
        services.AddHangfireServer(options =>
        {
            options.ServerCheckInterval = TimeSpan.FromMinutes(ApplicationConstants.Hangfire.ServerCheckIntervalMinutes);
            options.HeartbeatInterval = TimeSpan.FromSeconds(ApplicationConstants.Hangfire.HeartbeatIntervalSeconds);
            options.ServerTimeout = TimeSpan.FromMinutes(ApplicationConstants.Hangfire.ServerTimeoutMinutes);
            options.ShutdownTimeout = TimeSpan.FromMinutes(ApplicationConstants.Hangfire.ShutdownTimeoutMinutes);
        });

        return services;
    }

    /// <summary>
    /// Adds application-specific services including progress tracking and background services.
    /// </summary>
    /// <returns></returns>
    public static IServiceCollection AddApplicationSpecificServices(this IServiceCollection services)
    {
        // Add setup token service for first-run configuration
        services.AddScoped<ISetupTokenService, SetupTokenService>();

        // Add version service for application version information
        services.AddSingleton<IVersionService, VersionService>();

        // Add progress service for zip uploads
        services.AddScoped<IZipUploadProgressService, ZipUploadProgressService>();

        // Add zip upload job service for Hangfire
        services.AddScoped<IZipUploadJobService, ZipUploadJobService>();

        // Add item hierarchy service for resolving/creating collectible item hierarchies
        services.AddScoped<IItemHierarchyService, ItemHierarchyService>();

        // Add request log queue for async request logging (performance optimization)
        services.AddSingleton<Collectibles.Web.Services.RequestLogQueue>();

        // Add background service for processing request logs from queue
        services.AddHostedService<Collectibles.Web.Services.RequestLogBackgroundService>();

        // Add background service for preview generation (still using hosted service)
        services.AddHostedService<CollectibleItemPreviewBackgroundService>();

        // Add application lifetime logging service
        services.AddHostedService<ApplicationLifetimeService>();

        return services;
    }

    /// <summary>
    /// Configures all services required for the Collectibles application.
    /// </summary>
    /// <returns></returns>
    public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Identity services first (required by other services)
        services.AddIdentityServices(configuration);

        // Add core application services
        services.AddApplicationSpecificServices();

        // Add Infrastructure services
        services.AddInfrastructureServices(configuration);

        // Add Application services (MediatR, validators, handlers)
        services.AddApplicationServices();

        // Add Web-specific services
        services.AddWebServices();

        // Configure SyncTool settings and API key service
        services.Configure<SyncToolSettings>(configuration.GetSection("SyncTool"));
        services.AddSingleton<IApiKeyService, ApiKeyService>();

        // Throttle the credential-handling surface. Identity lockout guards a known
        // account; this adds a defence-in-depth limit at the HTTP layer, which the
        // first-run setup gate in particular had none of.
        //
        // A named policy only covers the endpoints that opt into it, so a global limiter
        // provides the baseline every route inherits, and the named policies below tighten
        // the surfaces that warrant it. Authenticated traffic partitions by user identity so
        // one account cannot spread load across many addresses; anonymous traffic partitions
        // by the resolved client address.
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(CreateGlobalRateLimitPartition);

            options.AddPolicy("AuthEndpoints", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ResolveRateLimitPartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = ApplicationConstants.RateLimits.AuthPermitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            // Anonymous, token-bearing routes: the tightest budget, because these are the
            // surfaces an unauthenticated caller can probe for valid tokens or hashes.
            options.AddPolicy("PublicEndpoints", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ResolveRateLimitPartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = ApplicationConstants.RateLimits.PublicPermitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            // Authenticated API surface, including uploads and downloads.
            options.AddPolicy("ApiEndpoints", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ResolveRateLimitPartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = ApplicationConstants.RateLimits.ApiPermitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        // Trusted-proxy handling must be configured before anything reads the client IP
        services.AddTrustedProxyForwardedHeaders(configuration);
        services.AddSingleton<IClientIpResolver, ClientIpResolver>();

        // Durable, named Data Protection keys
        services.AddDurableDataProtection(configuration);

        // Add authentication and authorization
        services.AddAuthenticationServices(configuration);

        // Add Hangfire services (with connection failure protection)
        services.AddHangfireServices(configuration);

        // Add Hangfire schema initializer (runs after database connectivity is confirmed)
        services.AddHangfireSchemaInitializer();

        return services;
    }

    /// <summary>
    /// Builds the global rate-limit partition, exempting framework and static-asset paths so a
    /// normal page load - which issues many requests for assets and Blazor plumbing - is not
    /// charged against a caller's budget.
    /// </summary>
    private static RateLimitPartition<string> CreateGlobalRateLimitPartition(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        foreach (var exempt in ApplicationConstants.RateLimits.ExemptPathPrefixes)
        {
            if (path.StartsWith(exempt, StringComparison.OrdinalIgnoreCase))
            {
                return RateLimitPartition.GetNoLimiter("exempt");
            }
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            ResolveRateLimitPartitionKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = ApplicationConstants.RateLimits.GlobalPermitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            });
    }

    /// <summary>
    /// Partitions rate limits by authenticated user where one is present, so a single account
    /// cannot spread its load across many addresses, and by client address otherwise. The
    /// connection address is used directly because forwarded-headers processing runs earlier in
    /// the pipeline and has already rewritten it for known proxies.
    /// </summary>
    private static string ResolveRateLimitPartitionKey(HttpContext context)
    {
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return !string.IsNullOrEmpty(userId)
            ? $"user:{userId}"
            : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }
}
