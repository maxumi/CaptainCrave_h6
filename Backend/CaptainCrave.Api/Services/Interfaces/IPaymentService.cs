using Api.DTOs;

namespace Api.Services;

// Definerer forretningslogik for det simulerede betalingssystem.
public interface IPaymentService
{
    // Behandler en simuleret betaling for en ordre og returnerer betalingsforsøget som DTO.
    Task<PaymentDto> ProcessPaymentAsync(CreatePaymentDto dto);

    // Henter det seneste betalingsforsøg for en ordre, eller null hvis der ikke findes et.
    Task<PaymentDto?> GetLatestByOrderIdAsync(int orderId);
}
