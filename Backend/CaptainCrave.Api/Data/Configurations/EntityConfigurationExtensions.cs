using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

// Indeholder fælles Fluent API-konfiguration for entities, der understøtter
// audit eller soft delete, så den samme konfiguration ikke gentages i hver klasse.
public static class EntityConfigurationExtensions
{
    // Konfigurerer created_at og updated_at for en entity, der implementerer IAuditable.
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

    // Konfigurerer is_deleted og deleted_at for en entity, der understøtter soft delete.
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
