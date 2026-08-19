using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

// Configures the menu_items table columns, constraints and relationships
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

        // Soft-deleted menu items are hidden from every normal query, and so are items of a soft-deleted menu or restaurant.
        builder.HasQueryFilter(m => !m.IsDeleted && !m.Menu.IsDeleted && !m.Menu.Restaurant.IsDeleted);

        builder.HasOne(m => m.Menu)
            .WithMany(mn => mn.MenuItems)
            .HasForeignKey(m => m.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Category)
            .WithMany(c => c.MenuItems)
            .HasForeignKey(m => m.CategoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}