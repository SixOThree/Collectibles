namespace Collectibles.Domain.Common.Entities;

public interface ISoftDelete
{
    DateTime? Deleted { get; set; }
    string? DeletedBy { get; set; }
}
