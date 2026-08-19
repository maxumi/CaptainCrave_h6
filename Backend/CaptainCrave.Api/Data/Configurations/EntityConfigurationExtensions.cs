using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

// Shared Fluent API setup for entities implementing IAuditable/ISoftDeletable, so every
// entity configuration does not repeat the same column setup.
public static class EntityConfigurationExtensions
{
    // Configures the created_at / updated_at columns for an auditable entity.
    public static void ConfigureAudit<T>(this EntityTypeBuilder<T> builder) where T : class, IAuditable
    {
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");
    }

    // Configures the is_deleted / deleted_at columns for a soft-deletable entity.
    public static void ConfigureSoftDelete<T>(this EntityTypeBuilder<T> builder) where T : class, ISoftDeletable
    {
        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at");
    }
}
