using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

// Denne klasse snakker direkte med databasen (via EF Core) og henter/gemmer betalinger.
public class PaymentRepository(AppDbContext db) : IPaymentRepository
{
    private readonly AppDbContext _db = db;

    /// <summary>Gemmer et nyt betalingsforsøg i databasen.</summary>
    /// <returns>Det gemte betalingsforsøg, nu med et rigtigt Id.</returns>
    public async Task<Payment> CreateAsync(Payment payment)
    {
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return payment;
    }

    /// <summary>Finder det seneste betalingsforsøg for en ordre, hvis der overhovedet er gjort et forsøg.</summary>
    /// <returns>Det seneste betalingsforsøg, eller null hvis der ikke er nogen.</returns>
    public async Task<Payment?> GetLatestByOrderIdAsync(int orderId) =>
        await _db.Payments
            .AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();
}
