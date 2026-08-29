namespace Collectibles.Web.Middleware;

public class CrawlerBlockingOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets user-Agent substrings to block. Case-insensitive matching.
    /// </summary>
    public List<string> BlockedUserAgents { get; set; } = new()
    {
        // Major AI crawlers
        "GPTBot",
        "ChatGPT-User",
        "CCBot",
        "Google-Extended",
        "anthropic-ai",
        "ClaudeBot",
        "Claude-Web",
        "Bytespider",
        "Diffbot",
        "FacebookBot",
        "Omgilibot",
        "Omgili",
        "YouBot",
        "PerplexityBot",
        "Timpibot",
        "Cohere-ai",
        "PetalBot",
        "ImagesiftBot",
        "Kangaroo Bot",
        "Meta-ExternalAgent",
        "Meta-ExternalFetcher",
        "Applebot-Extended",
        "AI2Bot",
        "Ai2Bot-Dolma",
        "iaskspider",
        "Scrapy",

        // SEO / marketing crawlers
        "AhrefsBot",
        "SemrushBot",
        "DotBot",
        "MJ12bot",
        "BLEXBot",
        "MegaIndex",
        "Sistrix",
        "SEOkicks",
        "seoscanners",
        "Rogerbot",
        "SeznamBot",
        "BacklinkCrawler",
        "Exabot",
        "linkdexbot",
        "Lipperhey",
        "SEOdiver",

        // Scraping / archiving bots
        "DataForSeoBot",
        "magpie-crawler",
        "Sogou",
        "Yandex",
        "Baiduspider",
        "serpstatbot",
        "ZoominfoBot",
        "Buck",
        "newspaper",
        "Gowikibot",
        "PaperLiBot",
        "Cliqzbot",
        "TurnitinBot",
        "ContentKing",
        "Screaming Frog",
        "Netcraft",

        // Vulnerability scanners / aggressive bots
        "Nmap",
        "sqlmap",
        "Nikto",
        "Openvas",
        "Masscan",
        "Wapiti",
        "Zgrab",
        "CensysInspect",
        "NetSystemsResearch",

        // Generic / low-value crawlers
        "crawl",
        "spider",
        "bot/",
        "Bot/",
        "Wget",
        "HTTrack",
        "libwww-perl",
        "python-requests",
        "python-urllib",
        "Go-http-client",
        "Java/",
        "okhttp",
        "httpx",
        "node-fetch",
        "axios",
        "curl/",
        "http_request2",
        "PHP/",
        "Ruby",
    };

    /// <summary>
    /// Gets or sets user-Agent substrings to allow even if they match a blocked pattern.
    /// Evaluated before the block list.
    /// </summary>
    public List<string> AllowedUserAgents { get; set; } = new()
    {
        // Allow standard browser engines so real users are never blocked
        "Mozilla",
        "Chrome",
        "Safari",
        "Firefox",
        "Edge",
        "Opera",

        // Allow specific useful bots
        "Googlebot",
        "bingbot",
        "Slurp",
        "DuckDuckBot",
    };

    /// <summary>
    /// Gets or sets a value indicating whether block requests with empty or missing User-Agent headers.
    /// </summary>
    public bool BlockEmptyUserAgent { get; set; } = true;
}
