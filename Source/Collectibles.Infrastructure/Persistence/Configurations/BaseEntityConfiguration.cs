using Collectibles.Domain.Common.Entities;

namespace Collectibles.Infrastructure.Persistence.Configurations;

/// <summary>
/// Base configuration for all entities that inherit from BaseEntity.
/// </summary>
public abstract class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T>
    where T : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        // Configure DomainEvents to be ignored by EF Core
        builder.Ignore(e => e.DomainEvents);

        // Call ConfigureEntity to allow derived configurations to add their specific configurations
        ConfigureEntity(builder);
    }

    /// <summary>
    /// Configure entity-specific mappings.
    /// </summary>
    protected abstract void ConfigureEntity(EntityTypeBuilder<T> builder);
}
