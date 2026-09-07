using Api.Models;

namespace Api.Repositories;

// Definerer databaseoperationer for simulerede betalingsforsøg.
public interface IPaymentRepository
{
    // Gemmer et nyt betalingsforsøg og returnerer det med det genererede ID.
    Task<Payment> CreateAsync(Payment payment);

    // Henter det seneste betalingsforsøg for en ordre, eller null hvis der ikke findes et.
    Task<Payment?> GetLatestByOrderIdAsync(int orderId);
}
