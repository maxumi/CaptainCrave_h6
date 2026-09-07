using Api.Models.Enums;

namespace Api.DTOs;

// Repræsenterer et betalingsforsøg, som returneres til klienten.
public class PaymentDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }

    // Reference til det simulerede betalingsforsøg.
    public string? ProviderReference { get; set; }
    public DateTime CreatedAt { get; set; }
}
