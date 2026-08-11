using Collectibles.Application.Common.Models;

namespace Collectibles.Application.Interfaces;

public interface IHomePageContentService
{
    /// <summary>Never throws: returns <see cref="HomePageContent.Default"/> on any failure.</summary>
    Task<HomePageContent> GetAsync();

    /// <summary>Validates and persists. Throws <see cref="ArgumentException"/> with a user-readable message when invalid.</summary>
    Task SaveAsync(HomePageContent content);
}
