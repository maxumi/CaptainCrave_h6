using Api.Models.Enums;

namespace Api.Models;

// Et enkelt (falsk) betalingsforsøg knyttet til en ordre. En ordre kan have flere forsøg,
// fx et fejlet forsøg efterfulgt af et nyt der lykkes.
public class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    // Falsk transaktions-id — ville svare til fx et Stripe payment intent-id i en rigtig integration.
    public string? ProviderReference { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
