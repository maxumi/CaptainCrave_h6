using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

// Konfigurerer menus-tabellens kolonner og relationer.
public class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.ToTable("menus");

        builder.HasKey(m => m.Id);

        // id: Primærnøglen genereres automatisk af databasen.
        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // restaurant_id: required FK, Hver menu skal være tilknyttet en restaurant.
        builder.Property(m => m.RestaurantId)
            .HasColumnName("restaurant_id")
            .IsRequired();

        // name: Menuens navn er påkrævet og må maksimalt være 100 tegn.
        builder.Property(m => m.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(100);

        builder.ConfigureAudit();
        builder.ConfigureSoftDelete();

        // SVENDEPRØVE – menuen skjules både, når den selv er slettet, og når dens parent
        // Restaurant er slettet. På den måde vises child-data ikke under en slettet restaurant.
        builder.HasQueryFilter(m => !m.IsDeleted && !m.Restaurant.IsDeleted);

        // En restaurant kan have flere menuer.
        // Hvis restauranten slettes permanent, slettes dens menuer også.
        builder.HasOne(m => m.Restaurant)
            .WithMany(r => r.Menus)
            .HasForeignKey(m => m.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
