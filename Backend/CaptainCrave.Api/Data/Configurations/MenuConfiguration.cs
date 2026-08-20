using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

// Configures the menus table columns and relationships
public class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.ToTable("menus");

        builder.HasKey(m => m.Id);

        // id: auto-generated primary key.
        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // restaurant_id: required FK, every menu must belong to exactly one restaurant.
        builder.Property(m => m.RestaurantId)
            .HasColumnName("restaurant_id")
            .IsRequired();

        // name: required display name, max 100 characters.
        builder.Property(m => m.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(100);

        builder.ConfigureAudit();
        builder.ConfigureSoftDelete();

        // Soft-deleted menus are hidden from every normal query, and so are menus of a soft-deleted restaurant.
        builder.HasQueryFilter(m => !m.IsDeleted && !m.Restaurant.IsDeleted);

        // One restaurant has many menus; deleting the restaurant deletes its menus too.
        builder.HasOne(m => m.Restaurant)
            .WithMany(r => r.Menus)
            .HasForeignKey(m => m.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
