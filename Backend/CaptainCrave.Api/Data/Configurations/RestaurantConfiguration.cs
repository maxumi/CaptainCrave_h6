using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

// Konfigurerer restaurants-tabellens kolonner, begrænsninger og relationer.
public class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> builder)
    {
        builder.ToTable("restaurants");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(r => r.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(r => r.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(r => r.Address)
            .HasColumnName("address")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(r => r.Latitude)
            .HasColumnName("latitude")
            .IsRequired();

        builder.Property(r => r.Longitude)
            .HasColumnName("longitude")
            .IsRequired();

        builder.Property(r => r.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(500);

        builder.Property(r => r.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.ConfigureAudit();
        builder.ConfigureSoftDelete();

        // SVENDEPRØVE – globalt query-filter: EF Core tilføjer automatisk denne betingelse
        // til normale forespørgsler. Derfor behøver hvert repository ikke huske !IsDeleted.
        builder.HasQueryFilter(r => !r.IsDeleted);

        // Relationer
        // En restaurant er tilknyttet én bruger.
        // Brugeren kan ikke slettes permanent, så længe restauranten refererer til den.
        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // En restaurant kan have flere menuer.
        // Hvis restauranten slettes permanent, slettes dens menuer også.
        builder.HasMany(r => r.Menus)
            .WithOne(m => m.Restaurant)
            .HasForeignKey(m => m.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
