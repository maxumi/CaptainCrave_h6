using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

// Repository snakker direkte med databasen .

// tager AppDbContext (databaseforbindelsen) ind.
public class PaymentRepository(AppDbContext db) : IPaymentRepository
{
    // DI: gemmer dependency i en private readonly field.
    private readonly AppDbContext _db = db;

    // Indsætter betalingen og returnerer den med det databasegenererede Id.
    public async Task<Payment> CreateAsync(Payment payment)
    {
        _db.Payments.Add(payment); // tilføjer batalingen til EF Core's change tracking system
        await _db.SaveChangesAsync(); // gemmer ændringen i databasen, der genereres Id'et
        return payment; 
    }

    // Henter det seneste betalingsforsøg for en given ordre.
    public async Task<Payment?> GetLatestByOrderIdAsync(int orderId) =>
        await _db.Payments
            .AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt) // Sorterer efter oprettelsestidspunkt.
            .FirstOrDefaultAsync(); // Henter første.
}
