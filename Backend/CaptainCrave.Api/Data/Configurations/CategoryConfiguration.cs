using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

// Konfigurerer categories-tabellens kolonner og relationer.
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.MenuId)
            .HasColumnName("menu_id")
            .IsRequired();

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(100);

        // Tilføjer de fælles felter til audit og soft delete.
        builder.ConfigureAudit();
        builder.ConfigureSoftDelete();

        // En kategori tilhører én menu, mens en menu kan have flere kategorier.
        builder.HasOne(c => c.Menu)
            .WithMany(m => m.Categories)
            .HasForeignKey(c => c.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        // Skjuler kategorier, hvis kategorien selv, dens menu eller restauranten er soft-deleted.
        builder.HasQueryFilter(c => !c.IsDeleted && !c.Menu.IsDeleted && !c.Menu.Restaurant.IsDeleted);
    }
}
