using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

// Konfigurerer menu_items-tabellens kolonner, begrænsninger og relationer.
public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("menu_items");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(m => m.MenuId)
            .HasColumnName("menu_id")
            .IsRequired();

        builder.Property(m => m.CategoryId)
            .HasColumnName("category_id");

        builder.Property(m => m.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(m => m.Price)
            .HasColumnName("price")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(m => m.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(500);

        builder.Property(m => m.IsAvailable)
            .HasColumnName("is_available")
            .IsRequired()
            .HasDefaultValue(true);

        builder.ConfigureAudit();
        builder.ConfigureSoftDelete();

        // Skjuler menu-items, hvis item'et selv, menuen eller restauranten er soft-deleted.
        builder.HasQueryFilter(m => !m.IsDeleted && !m.Menu.IsDeleted && !m.Menu.Restaurant.IsDeleted);

        // Et menu-item tilhører én menu, mens en menu kan indeholde flere menu-items.
        builder.HasOne(m => m.Menu)
            .WithMany(mn => mn.MenuItems)
            .HasForeignKey(m => m.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        // Et menu-item kan valgfrit tilhøre en kategori.
        // Kategorien kan ikke slettes permanent, så længe menu-items stadig refererer til den.
        builder.HasOne(m => m.Category)
            .WithMany(c => c.MenuItems)
            .HasForeignKey(m => m.CategoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}