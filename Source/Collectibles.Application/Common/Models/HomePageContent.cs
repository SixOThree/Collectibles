namespace Collectibles.Application.Common.Models;

/// <summary>
/// Editable content of the public home page: hero text plus an ordered list of
/// feature cards. Persisted as JSON in SiteConfiguration under "HomePage.Content".
/// The literal token {SiteTitle} in any field is replaced with the configured
/// site title at render time.
/// </summary>
public class HomePageContent
{
    public string HeroTitle { get; set; } = string.Empty;
    public string HeroLead { get; set; } = string.Empty;
    public List<HomePageCard> Cards { get; set; } = [];

    /// <summary>Replaces every occurrence of the {SiteTitle} token with the given site title.</summary>
    /// <returns></returns>
    public static string ReplaceSiteTitle(string text, string siteTitle) =>
        text.Replace("{SiteTitle}", siteTitle);

    /// <summary>
    /// Gets the hard-coded content the site ships with. Returns a fresh instance per
    /// access because callers (the admin page) mutate the returned object.
    /// </summary>
    public static HomePageContent Default => new()
    {
        HeroTitle = "An open-source showcase platform for collectors",
        HeroLead = "{SiteTitle} is a .NET / Blazor project for cataloging, sharing, and managing collections. Explore the source, run it yourself, or contribute.",
        Cards =
        [
            new HomePageCard
            {
                Icon = "bi-grid-3x3-gap",
                Title = "Template-Driven Fields",
                Text = "Collection templates define custom fields so each kind of collectible captures exactly the data that matters — text and large text areas, numbers, dates and date-times, booleans, dropdowns, and even inflation-adjusted prices.",
            },
            new HomePageCard
            {
                Icon = "bi-layers",
                Title = "Blazor & .NET",
                Text = "A .NET 10 Blazor Server front end built on Clean Architecture — Domain, Application, Infrastructure, and Web layers, with a MAUI Blazor Hybrid QR scanner companion.",
            },
            new HomePageCard
            {
                Icon = "bi-share",
                Title = "Public Sharing Routes",
                Text = "Showcases can be published to hash-id routes, letting collections be shared publicly without exposing internal identifiers.",
            },
            new HomePageCard
            {
                Icon = "bi-paperclip",
                Title = "Attachments & Media",
                Text = "Items support image and file attachments, with previews for many media types. Attachments can be uploaded in bulk via ZIP files with a hierarchical folder structure.",
            },
            new HomePageCard
            {
                Icon = "bi-palette",
                Title = "Theme & Configuration",
                Text = "A CSS-variable theme system and site settings drive styling and behavior without recompiling the application.",
            },
            new HomePageCard
            {
                Icon = "bi-speedometer2",
                Title = "Admin & Diagnostics",
                Text = "Management and diagnostics surfaces give administrators insight into the running application and its data.",
            },
        ],
    };
}

/// <summary>One feature card on the home page.</summary>
public class HomePageCard
{
    /// <summary>Gets or sets bootstrap icon name, e.g. "bi-grid-3x3-gap". Validated against <c>BootstrapIcons.All</c> on save.</summary>
    public string Icon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
