using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(oi => oi.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(oi => oi.MenuItemId)
            .HasColumnName("menu_item_id")
            .IsRequired();

        builder.Property(oi => oi.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(oi => oi.Price)
            .HasColumnName("price")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.HasOne(oi => oi.MenuItem)
            .WithMany()
            .HasForeignKey(oi => oi.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}