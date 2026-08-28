using Api.Models;

namespace Api.Repositories;

// Defines data access operations for (fake) payment attempts.
public interface IPaymentRepository
{
    // Saves a new payment attempt and returns it with the generated ID.
    Task<Payment> CreateAsync(Payment payment);

    // Returns the most recent payment attempt for an order, or null if none exist.
    Task<Payment?> GetLatestByOrderIdAsync(int orderId);
}
