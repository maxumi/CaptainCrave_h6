namespace Api.Models;

// Marks an entity as soft-deletable: a delete just hides the row instead of removing it, so it can be restored.
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}
