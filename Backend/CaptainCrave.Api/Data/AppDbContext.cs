using Api.Models;
using Api.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Api.Data;

// Databasekontekst for applikationen.
// Arver fra IdentityDbContext, så ASP.NET Core Identity og egne entities bruger samme database.
public class AppDbContext(DbContextOptions<AppDbContext> options)
: IdentityDbContext<User, IdentityRole<int>, int>(options)
{
    // DbSet-properties repræsenterer de tabeller, som EF Core arbejder med.
    public DbSet<Restaurant> Restaurants { get; set; }

    public DbSet<Menu> Menus { get; set; }

    public DbSet<Category> Categories { get; set; }

    public DbSet<MenuItem> MenuItems { get; set; }

    public DbSet<Order> Orders { get; set; }

    public DbSet<OrderItem> OrderItems { get; set; }

    public DbSet<Review> Reviews { get; set; }

    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Bevarer Identitys standardkonfiguration af brugere, roller og relaterede tabeller.
        base.OnModelCreating(modelBuilder);

        // Anvender de separate Fluent API-konfigurationer for applikationens entities.
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RestaurantConfiguration());
        modelBuilder.ApplyConfiguration(new MenuConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new MenuItemConfiguration());
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new OrderItemConfiguration());
        modelBuilder.ApplyConfiguration(new ReviewConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
    }
}
