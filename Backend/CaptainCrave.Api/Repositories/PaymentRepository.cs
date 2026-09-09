using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

// Denne klasse snakker direkte med databasen (via EF Core) og henter/gemmer betalinger.

// primary constructor, som tager AppDbContext (databaseforbindelsen) ind.
public class PaymentRepository(AppDbContext db) : IPaymentRepository
{
    // DI: gemmer databasekonteksten, så metoderne kan lave requests.
    private readonly AppDbContext _db = db;

    // Indsætter betalingen og returnerer den med det databasegenererede Id.
    public async Task<Payment> CreateAsync(Payment payment)
    {
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return payment;
    }

    // Henter det seneste betalingsforsøg for en given ordre.
    // AsNoTracking, da resultatet kun bruges til læsning, ikke opdatering.
    public async Task<Payment?> GetLatestByOrderIdAsync(int orderId) =>
        await _db.Payments
            .AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt) // Sorterer efter oprettelsestidspunkt.
            .FirstOrDefaultAsync(); // Henter det seneste betalingsforsøg for ordren.
}
