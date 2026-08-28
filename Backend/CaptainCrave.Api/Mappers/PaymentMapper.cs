using Api.DTOs;
using Api.Models;

namespace Api.Mappers;

// Extension methods for mapping Payment entities to DTOs.
public static class PaymentMapper
{
    // Maps a Payment entity to a PaymentDto for API responses.
    public static PaymentDto ToDto(this Payment payment) => new()
    {
        Id = payment.Id,
        OrderId = payment.OrderId,
        Amount = payment.Amount,
        Status = payment.Status,
        ProviderReference = payment.ProviderReference,
        CreatedAt = payment.CreatedAt
    };
}
