using Api.DTOs;
using Api.Models;

namespace Api.Mappers;

// Denne klasse hjælper os med at bygge om et betalingsforsøg (Payment) til en DTO,
// så vi kan sende det til klienten.
public static class PaymentMapper
{
    /// <summary>
    /// Tager et betalingsforsøg fra databasen og pakker det om til en PaymentDto:
    /// beløb, status (fx succes eller fejlet) og hvornår det skete.
    /// </summary>
    /// <returns>En PaymentDto klar til at blive sendt til klienten.</returns>
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
