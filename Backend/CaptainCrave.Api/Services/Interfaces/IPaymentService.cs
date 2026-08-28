using Api.DTOs;

namespace Api.Services;

// Defines business logic operations for (fake) payments.
public interface IPaymentService
{
    // Behandler en betaling for en ordre via den falske gateway, og returnerer forsøget som DTO.
    Task<PaymentDto> ProcessPaymentAsync(CreatePaymentDto dto);

    // Returns the latest payment attempt for an order, or null if none exist.
    Task<PaymentDto?> GetLatestByOrderIdAsync(int orderId);
}
