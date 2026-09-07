using Api.Data;
using Api.Models;
using Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

// Denne klasse snakker direkte med databasen (via EF Core) og henter/gemmer ordrer.
public class OrderRepository(AppDbContext db) : IOrderRepository
{
    private readonly AppDbContext _db = db;

    /// <summary>
    /// Henter en ordre sammen med bruger, restaurant, ordrelinjer og de retter, linjerne peger på.
    /// Vi springer slette-filteret på retter over her, så gammel ordrehistorik stadig virker,
    /// selvom en ret senere er blevet slettet fra menuen.
    /// </summary>
    /// <returns>Ordren med alle detaljer, eller null hvis den ikke findes.</returns>
    public async Task<Order?> GetByIdAsync(int id) =>
        await _db.Orders
            // SVENDEPRØVE – historiske ordrer skal stadig vise retter, som senere er soft-deleted.
            // Derfor omgår denne kontrollerede forespørgsel de globale filtre.
            .IgnoreQueryFilters()
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
            .Include(o => o.User)
            .Include(o => o.Restaurant)
            // Read-only forespørgsel: EF Core behøver ikke gemme en kopi til ændringssporing.
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);

    /// <summary>Gemmer en helt ny ordre i databasen. EF Core gemmer automatisk ordrelinjerne med.</summary>
    /// <returns>Den gemte ordre, nu med et rigtigt Id.</returns>
    public async Task<Order> CreateAsync(Order order)
    {
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    /// <summary>Skifter status på en ordre, der allerede findes, og opdaterer dens UpdatedAt-tidsstempel.</summary>
    /// <returns>True hvis ordren blev fundet og opdateret, false hvis den ikke findes.</returns>
    public async Task<bool> UpdateStatusAsync(int id, OrderStatus status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is null)
            return false;

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Henter alle ordrer, der stadig er i gang for en restaurant (dvs. ikke Delivered, Cancelled
    /// eller AwaitingPayment), sorteret så den nyest opdaterede kommer først. AwaitingPayment
    /// bliver holdt ude, så restauranten kun ser ordrer, hvor kunden faktisk har betalt.
    /// </summary>
    /// <returns>De ordrer, restauranten stadig skal arbejde med.</returns>
    public async Task<IEnumerable<Order>> GetActiveByRestaurantAsync(int restaurantId) =>
        await _db.Orders
            .IgnoreQueryFilters()
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
            .Include(o => o.User)
            .Include(o => o.Restaurant)
            .AsNoTracking()
            .Where(o => o.RestaurantId == restaurantId)
            .Where(o => o.Status != OrderStatus.Delivered
                && o.Status != OrderStatus.Cancelled
                && o.Status != OrderStatus.AwaitingPayment)
            .OrderByDescending(o => o.UpdatedAt)
            .ToListAsync();

    /// <summary>Henter alle færdige ordrer (Delivered eller Cancelled) for en restaurant, nyeste først.</summary>
    /// <returns>Restaurantens færdige ordrehistorik.</returns>
    public async Task<IEnumerable<Order>> GetHistoryByRestaurantAsync(int restaurantId) =>
        await _db.Orders
            .IgnoreQueryFilters()
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
            .Include(o => o.User)
            .Include(o => o.Restaurant)
            .AsNoTracking()
            .Where(o => o.RestaurantId == restaurantId)
            .Where(o => o.Status == OrderStatus.Delivered
                || o.Status == OrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();


    /// <summary>Finder brugerens ene ordre, der hverken er leveret eller annulleret, altså den, der er aktiv lige nu.</summary>
    /// <returns>Brugerens aktive ordre, eller null hvis brugeren ikke har en lige nu.</returns>
    public async Task<Order?> GetActiveOrderForUserAsync(int userId) =>
        await _db.Orders
            .IgnoreQueryFilters()
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
            .Include(o => o.User)
            .Include(o => o.Restaurant)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.UserId == userId
                && o.Status != OrderStatus.Delivered
                && o.Status != OrderStatus.Cancelled);

    /// <summary>Henter alle færdige ordrer (Delivered eller Cancelled) for en bruger, nyeste først.</summary>
    /// <returns>Brugerens færdige ordrehistorik.</returns>
    public async Task<IEnumerable<Order>> GetHistoryForUserAsync(int userId) =>
        await _db.Orders
            .IgnoreQueryFilters()
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
            .Include(o => o.User)
            .Include(o => o.Restaurant)
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .Where(o => o.Status == OrderStatus.Delivered
                || o.Status == OrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    /// <summary>
    /// Tjekker om en bruger har fået en ordre leveret fra en bestemt restaurant før. Kun
    /// leverede (Delivered) ordrer tæller, så man ikke kan anmelde en restaurant uden
    /// faktisk at have modtaget maden.
    /// </summary>
    /// <returns>True hvis brugeren har mindst én leveret ordre fra restauranten.</returns>
    public async Task<bool> HasUserOrderedFromRestaurantAsync(int userId, int restaurantId) =>
        await _db.Orders
            .IgnoreQueryFilters()
            .AnyAsync(o => o.UserId == userId
                && o.RestaurantId == restaurantId
                && o.Status == OrderStatus.Delivered);
}
