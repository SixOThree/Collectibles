namespace Collectibles.Web.Middleware;

public class SecurityScanBlockingOptions
{
    public List<string> SuspiciousPatterns { get; set; } = new()
    {
        @"wordpress|wp-admin|wp-content|wp-includes|wp-login",
        @"\.php$|\.asp$|\.aspx$|\.jsp$",
        @"phpmyadmin|pma|mysql|mysqladmin",

        // @"admin|administrator|manager|controlpanel",
        // @"manager|controlpanel",
        @"controlpanel",
        @"joomla|drupal|magento|prestashop",
        @"cgi-bin|scripts|fckeditor|ckfinder",
        @"\.git|\.svn|\.env|\.config",
        @"shell|cmd|eval|exec|system",

        // @"backup|dump|archive|old|temp",
        // @"test|demo|staging|dev"
    };

    public int MaxAttemptsBeforeBlock { get; set; } = 3;
    public int BlockDurationHours { get; set; } = 24;
    public TimeSpan BlockDuration => TimeSpan.FromHours(BlockDurationHours);
    public int TrackingWindowMinutes { get; set; } = 30;
    public bool UseForwardedHeaders { get; set; } = true;
    public bool Enabled { get; set; } = true;
}
