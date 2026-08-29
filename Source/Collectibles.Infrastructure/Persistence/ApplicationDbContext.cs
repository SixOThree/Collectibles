using System.Linq.Expressions;

using Collectibles.Application.Interfaces;
using Collectibles.Domain.Common;
using Collectibles.Domain.Common.Entities;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Collectibles.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUserService;

    /* Collectibles */
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<AttachmentContent> AttachmentContents { get; set; }
    public DbSet<AttachmentPreview> AttachmentPreviews { get; set; }
    public DbSet<CollectibleItem> CollectibleItems { get; set; }
    public DbSet<CollectibleItemAttachment> CollectibleItemAttachments { get; set; }
    public DbSet<CollectibleItemTag> CollectibleItemTags { get; set; }
    public DbSet<CollectibleItemRelatedTag> CollectibleItemRelatedTags { get; set; }
    public DbSet<ContentDefinition> ContentDefinitions { get; set; }
    public DbSet<QRCode> QRCodes { get; set; }
    public DbSet<LinkInfo> LinkInfos { get; set; }
    public DbSet<LinkCache> LinkCaches { get; set; }
    public DbSet<Showcase> Showcases { get; set; }
    public DbSet<ShowcaseTag> ShowcaseTags { get; set; }
    public DbSet<ShowcaseShareToken> ShowcaseShareTokens { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<TaxonomyTerm> TaxonomyTerms { get; set; }
    public DbSet<TaxonomyVocabulary> TaxonomyVocabularies { get; set; }

    /* Email */
    public DbSet<EmailLog> EmailLogs { get; set; }
    public DbSet<SiteConfiguration> SiteConfigurations { get; set; }

    /* Background Jobs */
    public DbSet<ZipUploadJob> ZipUploadJobs { get; set; }

    /* Logging */
    public DbSet<EventLog> EventLogs { get; set; }
    public DbSet<SysLog> SysLogs { get; set; }
    public DbSet<RequestLog> RequestLogs { get; set; }

    // Security
    public DbSet<Domain.Security.PasswordHistory> PasswordHistories { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    // Constructor for EF Core tools (migrations, etc.)
    // This constructor should only be used by EF Core design-time tools
    protected internal ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        _currentUserService = null!;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService?.UserId;
        var currentDateTime = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // Only set Created if not already set
                    entry.Entity.Created ??= currentDateTime;

                    // Only set CreatedBy if not already set (to preserve explicitly set values)
                    entry.Entity.CreatedBy ??= currentUserId;
                    break;

                case EntityState.Modified:
                    // Always update LastModified for modified entities
                    entry.Entity.LastModified = currentDateTime;

                    // Set LastModifiedBy to current user, or keep existing if no current user available
                    entry.Entity.LastModifiedBy = currentUserId ?? entry.Entity.LastModifiedBy;
                    break;
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        return result;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Ignore DomainEvent as it should not be mapped to any database table
        builder.Ignore<DomainEvent>();

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.ApiKeyHash).HasMaxLength(64);
            entity.HasIndex(u => u.ApiKeyHash).IsUnique().HasFilter("[ApiKeyHash] IS NOT NULL");
        });

        ApplySoftDeleteQueryFilters(builder);

        base.OnModelCreating(builder);
    }

    /// <summary>
    /// Enforces "deleted rows are invisible" once, centrally, for every
    /// <see cref="ISoftDelete"/> entity. Before this existed the predicate was written by
    /// hand at each call site, and the sites that forgot it silently served deleted content.
    /// Admin and restore paths that genuinely need deleted rows opt out with
    /// <c>IgnoreQueryFilters()</c>.
    /// </summary>
    private static void ApplySoftDeleteQueryFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType) || entityType.BaseType is not null)
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var deletedProperty = Expression.Property(parameter, nameof(ISoftDelete.Deleted));
            var filter = Expression.Lambda(
                Expression.Equal(deletedProperty, Expression.Constant(null, typeof(DateTime?))),
                parameter);

            builder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }
}
