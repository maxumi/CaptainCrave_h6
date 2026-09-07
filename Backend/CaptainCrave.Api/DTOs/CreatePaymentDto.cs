using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

// Data klienten sender for at gennemføre en simuleret betaling for en ordre.
// Beløbet modtages ikke fra klienten, men hentes fra ordren på serveren,
// så klienten ikke selv kan manipulere betalingsbeløbet.
public class CreatePaymentDto
{
    [Required]
    public int OrderId { get; set; }

    // Simuleret kortnummer der bruges til at afgøre,
    // om betalingsgatewayen skal simulere succes eller fejl.
    [Required]
    [MinLength(4, ErrorMessage = "Card number must be at least 4 digits.")]
    public string CardNumber { get; set; } = string.Empty;
}
