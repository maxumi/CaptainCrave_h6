using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

// Konfigurerer reviews-tabellens kolonner, begrænsninger og relationer.
public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(r => r.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(r => r.RestaurantId)
            .HasColumnName("restaurant_id")
            .IsRequired();

        builder.Property(r => r.Rating)
            .HasColumnName("rating")
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationer
        // En anmeldelse tilhører den bruger, der har oprettet den.
        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // En anmeldelse tilhører den restaurant, der er blevet anmeldt.
        // En restaurant kan have flere anmeldelser.
        builder.HasOne(r => r.Restaurant)
            .WithMany(r => r.Reviews)
            .HasForeignKey(r => r.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        // En bruger kan kun oprette én anmeldelse pr. restaurant.
        builder.HasIndex(r => new { r.UserId, r.RestaurantId })
            .IsUnique();

        // Sikrer på databaseniveau, at en vurdering altid ligger mellem 1 og 5. (check constraint)
        builder.ToTable(t =>
            t.HasCheckConstraint(
                "CK_reviews_rating",
                "[rating] >= 1 AND [rating] <= 5"));
    }
}