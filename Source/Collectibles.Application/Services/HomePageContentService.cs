using System.Text.Json;

using Collectibles.Application.Common;
using Collectibles.Application.Common.Models;
using Collectibles.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Services;

public class HomePageContentService(
    ISiteConfigurationService siteConfigurationService,
    ILogger<HomePageContentService> logger) : IHomePageContentService
{
    public const string ConfigurationKey = "HomePage.Content";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<HomePageContent> GetAsync()
    {
        try
        {
            var json = await siteConfigurationService.GetConfigurationValueAsync(ConfigurationKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                return HomePageContent.Default;
            }

            var content = JsonSerializer.Deserialize<HomePageContent>(json, SerializerOptions);
            return content ?? HomePageContent.Default;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load home page content; falling back to defaults");
            return HomePageContent.Default;
        }
    }

    public async Task SaveAsync(HomePageContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        Validate(content);

        var json = JsonSerializer.Serialize(content, SerializerOptions);
        await siteConfigurationService.SetConfigurationValueAsync(
            ConfigurationKey,
            json,
            "Home page hero text and feature cards (JSON)");
    }

    private static void Validate(HomePageContent content)
    {
        if (string.IsNullOrWhiteSpace(content.HeroTitle))
        {
            throw new ArgumentException("Hero title is required.", nameof(content));
        }

        if (string.IsNullOrWhiteSpace(content.HeroLead))
        {
            throw new ArgumentException("Hero lead is required.", nameof(content));
        }

        foreach (var card in content.Cards)
        {
            if (string.IsNullOrWhiteSpace(card.Title))
            {
                throw new ArgumentException("Every card requires a title.", nameof(content));
            }

            if (string.IsNullOrWhiteSpace(card.Text))
            {
                throw new ArgumentException("Every card requires text.", nameof(content));
            }

            if (!BootstrapIcons.IsValid(card.Icon))
            {
                throw new ArgumentException($"Card icon '{card.Icon}' is not a known Bootstrap icon.", nameof(content));
            }
        }
    }
}
