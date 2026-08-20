using Api.Models;

namespace Api.Repositories;

// Shared helpers for soft-deletable entities, so every repository does not repeat the same field updates.
public static class SoftDeleteHelper
{
    // Marks an entity as deleted and bumps its updated timestamp.
    public static void MarkDeleted<T>(T entity) where T : ISoftDeletable, IAuditable
    {
        var now = DateTime.UtcNow;
        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;
    }

    // Un-marks a previously deleted entity and bumps its updated timestamp.
    public static void MarkRestored<T>(T entity) where T : ISoftDeletable, IAuditable
    {
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.UpdatedAt = DateTime.UtcNow;
    }
}
