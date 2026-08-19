namespace Api.Models;

// Marks an entity as tracking when it was created and last changed.
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
