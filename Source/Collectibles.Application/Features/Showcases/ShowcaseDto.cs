namespace Collectibles.Application.Features.Showcases;

public class ShowcaseDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int ItemCount { get; set; }
    public List<Collectibles.Application.Features.Tags.TagDto> Tags { get; set; } = new();
    public DateTime CreatedDate { get; set; }
    public DateTime? LastModifiedDate { get; set; }
}

public class ShowcaseCardDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int ItemCount { get; set; }
    public List<string> TopTags { get; set; } = new();
    public string? UserId { get; set; }
    public string? OwnerDisplayName { get; set; }
    public bool IsPrivate { get; set; }
}
