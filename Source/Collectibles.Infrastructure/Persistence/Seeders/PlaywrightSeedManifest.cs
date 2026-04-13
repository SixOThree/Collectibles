namespace Collectibles.Infrastructure.Persistence.Seeders;

public sealed class PlaywrightSeedManifest
{
    public required SeedUsers Users { get; init; }
    public required SeedShowcases Showcases { get; init; }
    public required SeedItems Items { get; init; }
}

public sealed class SeedUsers
{
    public required SeedUser Admin { get; init; }
    public required SeedUser Regular { get; init; }
    public required SeedUser OtherOwner { get; init; }
}

public sealed class SeedUser
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string DisplayName { get; init; }
}

public sealed class SeedShowcases
{
    public required SeedReference RegularPrivate { get; init; }
    public required SeedReference RegularPublic { get; init; }
    public required SeedReference OtherPrivate { get; init; }
}

public sealed class SeedItems
{
    public required SeedReference RegularRoot { get; init; }
    public required SeedReference RegularChild { get; init; }
    public required SeedReference OtherPrivate { get; init; }
}

public sealed class SeedReference
{
    public required string Name { get; init; }
    public required string Hash { get; init; }
}
