using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

// EF Core implementation of payment data access.
public class PaymentRepository(AppDbContext db) : IPaymentRepository
{
    private readonly AppDbContext _db = db;

    // Inserts a new payment attempt.
    public async Task<Payment> CreateAsync(Payment payment)
    {
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return payment;
    }

    // Finds the newest payment attempt for the given order, if any.
    public async Task<Payment?> GetLatestByOrderIdAsync(int orderId) =>
        await _db.Payments
            .AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();
}
