using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

// Configures the categories table columns and relationships
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

        builder.ConfigureAudit();
        builder.ConfigureSoftDelete();

        builder.HasOne(c => c.Menu)
            .WithMany(m => m.Categories)
            .HasForeignKey(c => c.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        // Categories are hidden if deleted directly, or if their menu or restaurant is soft-deleted.
        builder.HasQueryFilter(c => !c.IsDeleted && !c.Menu.IsDeleted && !c.Menu.Restaurant.IsDeleted);
    }
}
