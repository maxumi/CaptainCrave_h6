using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

// Configures the reviews table and its relationships.
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

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Restaurant)
            .WithMany(r => r.Reviews)
            .HasForeignKey(r => r.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        // A user can only review the same restaurant once.
        builder.HasIndex(r => new { r.UserId, r.RestaurantId })
            .IsUnique();

        builder.ToTable(t =>
            t.HasCheckConstraint(
                "CK_reviews_rating",
                "[rating] >= 1 AND [rating] <= 5"));
    }
}