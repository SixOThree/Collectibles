using Collectibles.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Interfaces;

public interface IApplicationDbContext : IAsyncDisposable
{
    /* Collectibles */
    DbSet<Attachment> Attachments { get; set; }
    DbSet<AttachmentContent> AttachmentContents { get; set; }
    DbSet<AttachmentPreview> AttachmentPreviews { get; set; }
    DbSet<CollectibleItem> CollectibleItems { get; set; }
    DbSet<CollectibleItemAttachment> CollectibleItemAttachments { get; set; }
    DbSet<CollectibleItemTag> CollectibleItemTags { get; set; }
    DbSet<CollectibleItemRelatedTag> CollectibleItemRelatedTags { get; set; }
    DbSet<ContentDefinition> ContentDefinitions { get; set; }
    DbSet<LinkInfo> LinkInfos { get; set; }
    DbSet<LinkCache> LinkCaches { get; set; }
    DbSet<Showcase> Showcases { get; set; }
    DbSet<ShowcaseTag> ShowcaseTags { get; set; }
    DbSet<Tag> Tags { get; set; }
    DbSet<TaxonomyTerm> TaxonomyTerms { get; set; }
    DbSet<TaxonomyVocabulary> TaxonomyVocabularies { get; set; }

    /* Email */
    DbSet<EmailLog> EmailLogs { get; set; }
    DbSet<SiteConfiguration> SiteConfigurations { get; set; }

    /* Background Jobs */
    DbSet<ZipUploadJob> ZipUploadJobs { get; set; }

    /* Logging */
    DbSet<EventLog> EventLogs { get; set; }
    DbSet<SysLog> SysLogs { get; set; }
    DbSet<RequestLog> RequestLogs { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
