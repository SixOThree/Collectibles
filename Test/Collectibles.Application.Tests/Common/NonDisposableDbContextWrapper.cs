namespace Collectibles.Application.Tests.Common;

/// <summary>
/// A wrapper around IApplicationDbContext that prevents disposal.
/// Used in tests to prevent the handlers from disposing the shared test context.
/// </summary>
public class NonDisposableDbContextWrapper : IApplicationDbContext
{
    private readonly IApplicationDbContext _innerContext;

    public NonDisposableDbContextWrapper(IApplicationDbContext innerContext)
    {
        _innerContext = innerContext;
    }

    // IApplicationDbContext implementation - delegate all properties to inner context
    public DbSet<Attachment> Attachments
    {
        get => _innerContext.Attachments;
        set => _innerContext.Attachments = value;
    }

    public DbSet<AttachmentContent> AttachmentContents
    {
        get => _innerContext.AttachmentContents;
        set => _innerContext.AttachmentContents = value;
    }

    public DbSet<AttachmentPreview> AttachmentPreviews
    {
        get => _innerContext.AttachmentPreviews;
        set => _innerContext.AttachmentPreviews = value;
    }

    public DbSet<CollectibleItem> CollectibleItems
    {
        get => _innerContext.CollectibleItems;
        set => _innerContext.CollectibleItems = value;
    }

    public DbSet<CollectibleItemAttachment> CollectibleItemAttachments
    {
        get => _innerContext.CollectibleItemAttachments;
        set => _innerContext.CollectibleItemAttachments = value;
    }

    public DbSet<CollectibleItemTag> CollectibleItemTags
    {
        get => _innerContext.CollectibleItemTags;
        set => _innerContext.CollectibleItemTags = value;
    }

    public DbSet<CollectibleItemRelatedTag> CollectibleItemRelatedTags
    {
        get => _innerContext.CollectibleItemRelatedTags;
        set => _innerContext.CollectibleItemRelatedTags = value;
    }

    public DbSet<ContentDefinition> ContentDefinitions
    {
        get => _innerContext.ContentDefinitions;
        set => _innerContext.ContentDefinitions = value;
    }

    public DbSet<LinkInfo> LinkInfos
    {
        get => _innerContext.LinkInfos;
        set => _innerContext.LinkInfos = value;
    }

    public DbSet<LinkCache> LinkCaches
    {
        get => _innerContext.LinkCaches;
        set => _innerContext.LinkCaches = value;
    }

    public DbSet<Showcase> Showcases
    {
        get => _innerContext.Showcases;
        set => _innerContext.Showcases = value;
    }

    public DbSet<ShowcaseTag> ShowcaseTags
    {
        get => _innerContext.ShowcaseTags;
        set => _innerContext.ShowcaseTags = value;
    }

    public DbSet<Tag> Tags
    {
        get => _innerContext.Tags;
        set => _innerContext.Tags = value;
    }

    public DbSet<TaxonomyTerm> TaxonomyTerms
    {
        get => _innerContext.TaxonomyTerms;
        set => _innerContext.TaxonomyTerms = value;
    }

    public DbSet<TaxonomyVocabulary> TaxonomyVocabularies
    {
        get => _innerContext.TaxonomyVocabularies;
        set => _innerContext.TaxonomyVocabularies = value;
    }

    public DbSet<EmailLog> EmailLogs
    {
        get => _innerContext.EmailLogs;
        set => _innerContext.EmailLogs = value;
    }

    public DbSet<SiteConfiguration> SiteConfigurations
    {
        get => _innerContext.SiteConfigurations;
        set => _innerContext.SiteConfigurations = value;
    }

    public DbSet<ZipUploadJob> ZipUploadJobs
    {
        get => _innerContext.ZipUploadJobs;
        set => _innerContext.ZipUploadJobs = value;
    }

    public DbSet<EventLog> EventLogs
    {
        get => _innerContext.EventLogs;
        set => _innerContext.EventLogs = value;
    }

    public DbSet<SysLog> SysLogs
    {
        get => _innerContext.SysLogs;
        set => _innerContext.SysLogs = value;
    }

    public DbSet<RequestLog> RequestLogs
    {
        get => _innerContext.RequestLogs;
        set => _innerContext.RequestLogs = value;
    }

    // Methods
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _innerContext.SaveChangesAsync(cancellationToken);
    }

    // Dispose method - do nothing to prevent disposal
    public ValueTask DisposeAsync()
    {
        // Do nothing - prevent disposal
        return ValueTask.CompletedTask;
    }
}
