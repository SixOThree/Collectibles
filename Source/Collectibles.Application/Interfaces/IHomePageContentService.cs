using Collectibles.Application.Common.Models;

namespace Collectibles.Application.Interfaces;

public interface IHomePageContentService
{
    /// <summary>Never throws: returns <see cref="HomePageContent.Default"/> on any failure.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<HomePageContent> GetAsync();

    /// <summary>Validates and persists. Throws <see cref="ArgumentException"/> with a user-readable message when invalid.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SaveAsync(HomePageContent content);
}
