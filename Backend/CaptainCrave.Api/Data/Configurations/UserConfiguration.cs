using Api.Models;
using Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

// Configures the users table columns, constraints and indexes using EF Core fluent API
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        // builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(u => u.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(255);

        // builder.HasIndex(u => u.Email)
        //     .IsUnique();

        builder.Property(u => u.Address)
            .HasColumnName("address")
            .HasMaxLength(255);

        builder.Property(u => u.Latitude)
            .HasColumnName("latitude");

        builder.Property(u => u.Longitude)
            .HasColumnName("longitude");

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .IsRequired()
            .HasConversion<string>();

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAdd();
    }
}
