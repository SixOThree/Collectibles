using System.Reflection;

using Collectibles.Application.Behaviors;
using Collectibles.Application.Common.Authorization.Handlers;
using Collectibles.Application.Common.Services;
using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;
using Collectibles.Application.Services;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Collectibles.Application.Common;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // MediatR and FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        });

        // Register authorization handlers
        services.AddScoped<IAuthorizationHandler, ShowcaseAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, EditShowcaseAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, DeleteShowcaseAuthorizationHandler>();

        services.AddScoped<IAuthorizationHandler, ViewCollectibleItemAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, EditCollectibleItemAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, DeleteCollectibleItemAuthorizationHandler>();

        services.AddScoped<IAuthorizationHandler, ViewAttachmentAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, EditAttachmentAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, DeleteAttachmentAuthorizationHandler>();

        // Register application services
        services.AddSingleton<IInflationCalculationService, InflationCalculationService>();
        services.AddScoped<ISiteConfigurationService, SiteConfigurationService>();

        // Register explicit mapping services
        services.AddScoped<IAttachmentMappingService, AttachmentMappingService>();
        services.AddScoped<ICollectibleItemMappingService, CollectibleItemMappingService>();
        services.AddScoped<IShowcaseMappingService, ShowcaseMappingService>();

        return services;
    }
}
