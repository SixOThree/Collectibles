namespace Collectibles.Domain.Constants;

public static class ApplicationConstants
{
    public static class FileUpload
    {
        /// <summary>
        /// Maximum number of files that can be uploaded at once.
        /// </summary>
        public const int MaxFilesPerUpload = 5000;

        /// <summary>
        /// Maximum file size in bytes (2 GB)
        /// Note: This is multiplied by 10 in ZipUploadBackgroundSimple.razor for a total of 20 GB.
        /// </summary>
        public const long MaxFileSize = 2L * 1024 * 1024 * 1024;
    }

    public static class Theme
    {
        /// <summary>
        /// Default theme for the application.
        /// </summary>
        public const string DefaultTheme = "cosmo";
    }

    public static class Caching
    {
        /// <summary>
        /// Cache duration for attachment previews in minutes.
        /// </summary>
        public const int AttachmentPreviewCacheMinutes = 30;

        /// <summary>
        /// Cache duration for user information in seconds.
        /// </summary>
        public const int UserCacheSeconds = 30;
    }

    public static class BackgroundServices
    {
        /// <summary>
        /// Interval for collectible preview background service in minutes.
        /// </summary>
        public const int CollectiblePreviewCheckMinutes = 5;

        /// <summary>
        /// Initial delay for collectible preview service in seconds.
        /// </summary>
        public const int CollectiblePreviewInitialDelaySeconds = 30;

        /// <summary>
        /// Interval for link processor service in minutes.
        /// </summary>
        public const int LinkProcessorIntervalMinutes = 1;

        /// <summary>
        /// Interval for ZIP upload background service in seconds.
        /// </summary>
        public const int ZipUploadCheckSeconds = 5;
    }

    /// <summary>
    /// Version and build-related constants.
    /// </summary>
    public static class Version
    {
        public const string DefaultVersion = "0.0.0.0";
        public const string UnknownEnvironment = "Unknown";
        public const string DateFormat = "yyyy-MM-dd HH:mm:ss";
        public const string ShortDateFormat = "MMM dd, yyyy";
        public const int RefreshIntervalSeconds = 300; // 5 minutes
    }

    public static class Web
    {
        /// <summary>
        /// Request headers timeout in minutes.
        /// </summary>
        public const int RequestHeadersTimeoutMinutes = 10;

        /// <summary>
        /// Keep-alive timeout in minutes.
        /// </summary>
        public const int KeepAliveTimeoutMinutes = 10;

        /// <summary>
        /// JS interop default call timeout in seconds.
        /// </summary>
        public const int JSInteropTimeoutSeconds = 60;

        /// <summary>
        /// Disconnected circuit retention period in minutes.
        /// </summary>
        public const int CircuitRetentionMinutes = 3;

        /// <summary>
        /// SignalR client timeout interval in minutes.
        /// </summary>
        public const int SignalRClientTimeoutMinutes = 10;

        /// <summary>
        /// SignalR keep-alive interval in seconds.
        /// </summary>
        public const int SignalRKeepAliveSeconds = 15;

        /// <summary>
        /// SignalR handshake timeout in seconds.
        /// </summary>
        public const int SignalRHandshakeSeconds = 15;

        /// <summary>
        /// SignalR maximum receive message size in bytes (32 MB).
        /// </summary>
        public const long SignalRMaxMessageSizeBytes = 33_554_432; // 32 MB

        /// <summary>
        /// SignalR stream buffer capacity.
        /// </summary>
        public const int SignalRStreamBufferCapacity = 10;
    }

    public static class Identity
    {
        /// <summary>
        /// Authentication state revalidation interval in minutes.
        /// </summary>
        public const int AuthRevalidationMinutes = 30;

        /// <summary>
        /// Password reset token lifespan in hours.
        /// </summary>
        public const int PasswordResetTokenHours = 3;

        /// <summary>
        /// Identity status cookie max age in seconds.
        /// </summary>
        public const int StatusCookieMaxAgeSeconds = 5;

        /// <summary>
        /// Account lockout duration in minutes (configurable).
        /// </summary>
        public const int DefaultLockoutMinutes = 15;
    }

    public static class Database
    {
        /// <summary>
        /// SQL command batch max timeout in minutes.
        /// </summary>
        public const int CommandBatchMaxTimeoutMinutes = 5;

        /// <summary>
        /// Sliding invisibility timeout in minutes.
        /// </summary>
        public const int SlidingInvisibilityTimeoutMinutes = 5;

        /// <summary>
        /// Maximum retry delay for database operations in seconds.
        /// </summary>
        public const int MaxRetryDelaySeconds = 30;

        /// <summary>
        /// Connection timeout in seconds.
        /// </summary>
        public const int ConnectionTimeoutSeconds = 30;
    }

    public static class Hangfire
    {
        /// <summary>
        /// Server check interval in minutes.
        /// </summary>
        public const int ServerCheckIntervalMinutes = 1;

        /// <summary>
        /// Heartbeat interval in seconds.
        /// </summary>
        public const int HeartbeatIntervalSeconds = 30;

        /// <summary>
        /// Server timeout in minutes.
        /// </summary>
        public const int ServerTimeoutMinutes = 5;

        /// <summary>
        /// Shutdown timeout in minutes.
        /// </summary>
        public const int ShutdownTimeoutMinutes = 1;
    }

    public static class Media
    {
        /// <summary>
        /// Video thumbnail capture time in seconds.
        /// </summary>
        public const int VideoThumbnailCaptureSeconds = 1;

        /// <summary>
        /// Maximum time the ffmpeg snapshot for one video may run before it is abandoned.
        /// Without a bound, a malformed video can hold a child process open indefinitely.
        /// </summary>
        public const int VideoThumbnailTimeoutSeconds = 60;

        /// <summary>
        /// Largest image dimension (width or height) accepted for decoding. A small file
        /// can declare enormous dimensions and force a width x height x 4 allocation.
        /// </summary>
        public const int MaxImageDimension = 20000;

        /// <summary>
        /// Largest total pixel count accepted for decoding.
        /// </summary>
        public const long MaxImagePixels = 100_000_000;

        /// <summary>
        /// Thumbnail width in pixels.
        /// </summary>
        public const int ThumbnailWidth = 512;

        /// <summary>
        /// Thumbnail height in pixels.
        /// </summary>
        public const int ThumbnailHeight = 512;

        /// <summary>
        /// PDF rendering DPI.
        /// </summary>
        public const int PdfRenderDpi = 150;

        /// <summary>
        /// Maximum paragraphs to extract from Word documents.
        /// </summary>
        public const int WordDocumentMaxParagraphs = 10;

        /// <summary>
        /// Maximum text elements to extract from PowerPoint slides.
        /// </summary>
        public const int PowerPointMaxTextElements = 5;

        /// <summary>
        /// Number of images for collage creation.
        /// </summary>
        public const int CollageImageCount = 4;

        /// <summary>
        /// Degrees for normalizing image rotation.
        /// </summary>
        public const int RotationNormalizeDegrees = 360;
    }

    /// <summary>
    /// HTTP caching constants.
    /// </summary>
    public static class HttpCache
    {
        /// <summary>
        /// Cache duration for public attachments in seconds (7 days).
        /// </summary>
        public const int PublicAttachmentCacheSeconds = 604800;

        /// <summary>
        /// Cache-Control header value for public attachments.
        /// </summary>
        public const string PublicAttachmentCacheHeader = "public, max-age=604800";
    }

    /// <summary>
    /// Batch processing size constants.
    /// </summary>
    public static class BatchProcessing
    {
        /// <summary>
        /// Default batch size for most operations.
        /// </summary>
        public const int DefaultBatchSize = 100;

        /// <summary>
        /// Small batch size for preview generation.
        /// </summary>
        public const int PreviewBatchSize = 20;

        /// <summary>
        /// Batch size for attachment indexing.
        /// </summary>
        public const int AttachmentIndexingBatchSize = 50;

        /// <summary>
        /// Batch size for email processing.
        /// </summary>
        public const int EmailBatchSize = 50;

        /// <summary>
        /// Large batch size for cleanup operations.
        /// </summary>
        public const int CleanupBatchSize = 1000;

        /// <summary>
        /// Batch size for SendGrid messages.
        /// </summary>
        public const int SendGridBatchSize = 1000;

        /// <summary>
        /// Delay between request log batches in milliseconds.
        /// </summary>
        public const int RequestLogBatchDelayMs = 1000;

        /// <summary>
        /// Queue capacity for request logging.
        /// </summary>
        public const int RequestLogQueueCapacity = 1000;
    }

    /// <summary>
    /// Pagination constants.
    /// </summary>
    public static class Pagination
    {
        /// <summary>
        /// Default page size for queries.
        /// </summary>
        public const int DefaultPageSize = 100;

        /// <summary>
        /// Preview items to show in showcase cards.
        /// </summary>
        public const int ShowcasePreviewItemCount = 3;
    }

    /// <summary>
    /// API route constants.
    /// </summary>
    public static class ApiRoutes
    {
        /// <summary>
        /// Base path for attachment API.
        /// </summary>
        public const string AttachmentApiBase = "/api/attachments";

        /// <summary>
        /// Base path for public API.
        /// </summary>
        public const string PublicApiBase = "/api/public";

        /// <summary>
        /// Path format for attachment URLs.
        /// </summary>
        public const string AttachmentUrlPath = "/api/attachments/";

        /// <summary>
        /// Path format for public attachment URLs.
        /// </summary>
        public const string PublicAttachmentUrlPath = "/api/public/attachments/";

        /// <summary>
        /// QR code route prefix.
        /// </summary>
        public const string QrCodeRoutePrefix = "/qr/";

        /// <summary>
        /// Error route pattern.
        /// </summary>
        public const string ErrorRoutePattern = "/error/{0}";

        /// <summary>
        /// MiniProfiler route base path.
        /// </summary>
        public const string MiniProfilerRouteBase = "/profiler";
    }

    /// <summary>
    /// Validation length constants.
    /// </summary>
    public static class ValidationLengths
    {
        /// <summary>
        /// Standard name field max length.
        /// </summary>
        public const int NameMaxLength = 200;

        /// <summary>
        /// Standard description max length.
        /// </summary>
        public const int DescriptionMaxLength = 1000;

        /// <summary>
        /// Extended description max length (for showcases).
        /// </summary>
        public const int ExtendedDescriptionMaxLength = 2000;

        /// <summary>
        /// Short name max length (for tags, first/last names).
        /// </summary>
        public const int ShortNameMaxLength = 50;

        /// <summary>
        /// Type field max length.
        /// </summary>
        public const int TypeMaxLength = 100;

        /// <summary>
        /// File name max length.
        /// </summary>
        public const int FileNameMaxLength = 255;

        /// <summary>
        /// Email max length.
        /// </summary>
        public const int EmailMaxLength = 256;

        /// <summary>
        /// Request signature max length.
        /// </summary>
        public const int RequestSignatureMaxLength = 1024;

        /// <summary>
        /// URL max length.
        /// </summary>
        public const int UrlMaxLength = 2048;

        /// <summary>
        /// Share-link token max length. Tokens are 44 characters; the column is bounded so
        /// it can carry a unique index.
        /// </summary>
        public const int ShareTokenMaxLength = 64;

        /// <summary>
        /// Configuration key max length.
        /// </summary>
        public const int ConfigurationKeyMaxLength = 200;
    }

    /// <summary>
    /// Authorization role constants.
    /// </summary>
    public static class Roles
    {
        /// <summary>
        /// Administrator role name.
        /// </summary>
        public const string Administrator = "Administrator";

        /// <summary>
        /// User manager role name.
        /// </summary>
        public const string UserManager = "UserManager";

        /// <summary>
        /// Viewer role name.
        /// </summary>
        public const string Viewer = "Viewer";
    }

    /// <summary>
    /// Request body size limits.
    /// </summary>
    public static class RequestLimits
    {
        /// <summary>
        /// Maximum request body size (20 GB).
        /// </summary>
        public const long MaxRequestBodySize = 21_474_836_480;

        /// <summary>
        /// Maximum disconnected circuits to retain.
        /// </summary>
        public const int MaxDisconnectedCircuits = 100;

        /// <summary>
        /// Minimum thread pool threads.
        /// </summary>
        public const int MinThreadPoolThreads = 200;

        /// <summary>
        /// Maximum Azure blob transfer size (100 MB).
        /// </summary>
        public const long MaxAzureTransferSize = 100 * 1024 * 1024;

        /// <summary>
        /// Direct upload size threshold (50 MB).
        /// </summary>
        public const long DirectUploadThreshold = 50 * 1024 * 1024;

        /// <summary>
        /// SAS token expiry in minutes.
        /// </summary>
        public const int SasExpiryMinutes = 30;
    }

    /// <summary>
    /// Time-based operation constants.
    /// </summary>
    public static class TimeOperations
    {
        /// <summary>
        /// Hours after which orphaned jobs are detected.
        /// </summary>
        public const int OrphanedJobHours = 1;

        /// <summary>
        /// Days to retain email logs.
        /// </summary>
        public const int EmailLogRetentionDays = 90;

        /// <summary>
        /// Minutes for event log search time window (before/after).
        /// </summary>
        public const int EventLogSearchWindowMinutes = 30;

        /// <summary>
        /// Days for log file retention.
        /// </summary>
        public const int LogFileRetentionDays = 30;

        /// <summary>
        /// Days a soft-deleted attachment stays restorable before it (and its storage
        /// files) are permanently reclaimed by the purge job.
        /// </summary>
        public const int DeletedAttachmentRetentionDays = 30;
    }

    /// <summary>
    /// Password validation constants.
    /// </summary>
    public static class PasswordValidation
    {
        /// <summary>
        /// Minimum password part length for validation.
        /// </summary>
        public const int MinPartLength = 3;

        /// <summary>
        /// Minimum entropy threshold for password complexity.
        /// </summary>
        public const double MinEntropyThreshold = 50.0;

        /// <summary>
        /// Threshold for repeated character detection.
        /// </summary>
        public const int RepeatedCharThreshold = 3;

        /// <summary>
        /// Minimum substring length for sequential character checking.
        /// </summary>
        public const int SequentialCheckLength = 4;

        /// <summary>
        /// Character set size for lowercase letters.
        /// </summary>
        public const int LowercaseCharSetSize = 26;

        /// <summary>
        /// Character set size for uppercase letters.
        /// </summary>
        public const int UppercaseCharSetSize = 26;

        /// <summary>
        /// Character set size for digits.
        /// </summary>
        public const int DigitCharSetSize = 10;

        /// <summary>
        /// Character set size for special characters.
        /// </summary>
        public const int SpecialCharSetSize = 32;

        /// <summary>
        /// Maximum failed access attempts before lockout.
        /// </summary>
        public const int MaxFailedAccessAttempts = 5;
    }

    /// <summary>
    /// Logging configuration constants.
    /// </summary>
    public static class Logging
    {
        /// <summary>
        /// Log file directory.
        /// </summary>
        public const string LogDirectory = "Logs";

        /// <summary>
        /// Main log file pattern.
        /// </summary>
        public const string MainLogFilePattern = "collectibles-.txt";

        /// <summary>
        /// Error log file pattern.
        /// </summary>
        public const string ErrorLogFilePattern = "errors-.txt";

        /// <summary>
        /// SQL command timeout in seconds.
        /// </summary>
        public const int SqlCommandTimeoutSeconds = 60;

        /// <summary>
        /// Query execution warning threshold in milliseconds.
        /// </summary>
        public const int QueryWarningThresholdMs = 100;
    }

    /// <summary>
    /// Email configuration constants.
    /// </summary>
    public static class Email
    {
        /// <summary>
        /// Default SMTP port.
        /// </summary>
        public const int DefaultSmtpPort = 587;

        /// <summary>
        /// Default SMTP timeout in seconds.
        /// </summary>
        public const int DefaultSmtpTimeoutSeconds = 30;

        /// <summary>
        /// Azure Communication Services timeout in seconds.
        /// </summary>
        public const int AzureCommunicationTimeoutSeconds = 120;

        /// <summary>
        /// Maximum retry delay in seconds.
        /// </summary>
        public const int MaxRetryDelaySeconds = 300;
    }

    /// <summary>
    /// MiniProfiler path ignore patterns.
    /// </summary>
    public static class ProfilerIgnorePaths
    {
        public const string Css = "/css";
        public const string Js = "/js";
        public const string Lib = "/lib";
        public const string Themes = "/themes";
        public const string AppCss = "/app.css";
        public const string Framework = "/_framework";
        public const string Blazor = "/_blazor";
    }
}
