using Api.Models;
using Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(p => p.Amount)
            .HasColumnName("amount")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(PaymentStatus.Pending);

        builder.Property(p => p.ProviderReference)
            .HasColumnName("provider_reference")
            .HasMaxLength(100);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
