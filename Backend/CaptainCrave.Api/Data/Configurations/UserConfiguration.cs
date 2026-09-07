using Api.Models;
using Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

// Konfigurerer users-tabellens egne felter ved hjælp af EF Core Fluent API.
// Identity håndterer de øvrige standardfelter og konfigurationer for brugeren.
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

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

        // Gemmer brugerens rolle som tekst i databasen.
        builder.Property(u => u.Role)
            .HasColumnName("role")
            .IsRequired()
            .HasConversion<string>();

        // Tilføjer de fælles audit-felter til brugeren.
        builder.ConfigureAudit();
    }
}
