using Api.Models;
using Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

// Konfigurerer orders-tabellens kolonner og relationer.
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(o => o.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(o => o.RestaurantId)
            .HasColumnName("restaurant_id")
            .IsRequired();

        // Gemmer ordrestatus som tekst i databasen i stedet for en numerisk enum-værdi.
        builder.Property(o => o.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(OrderStatus.Pending);

        // Gemmer leveringstypen som tekst i databasen.
        builder.Property(o => o.DeliveryType)
            .HasColumnName("delivery_type")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(o => o.DeliveryAddress)
            .HasColumnName("delivery_address")
            .HasMaxLength(500);

        builder.Property(o => o.TotalPrice)
            .HasColumnName("total_price")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(o => o.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationer
        // En ordre tilhører én bruger.
        // Restrict forhindrer sletning af brugeren, hvis der stadig findes relaterede ordrer.
        builder.HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // En ordre tilhører én restaurant.
        // Restauranten kan ikke slettes permanent, hvis den stadig har relaterede ordrer.
        builder.HasOne(o => o.Restaurant)
            .WithMany()
            .HasForeignKey(o => o.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        // En ordre kan have flere order-items.
        // Hvis ordren slettes permanent, slettes dens order-items også.
        builder.HasMany(o => o.OrderItems)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // En ordre kan have flere betalingsforsøg (fx et fejlet forsøg efterfulgt af et der lykkes).
        builder.HasMany(o => o.Payments)
            .WithOne(p => p.Order)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}