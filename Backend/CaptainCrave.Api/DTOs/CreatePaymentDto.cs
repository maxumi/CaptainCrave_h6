using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

// Data klienten sender for at gennemføre en (falsk) betaling for en ordre.
// Beløbet tages IKKE fra klienten — det slås op på ordren server-side, så det ikke kan manipuleres.
public class CreatePaymentDto
{
    [Required]
    public int OrderId { get; set; }

    // Falsk kortnummer, bruges kun til at afgøre om den falske gateway simulerer succes eller fejl.
    [Required]
    [MinLength(4, ErrorMessage = "Card number must be at least 4 digits.")]
    public string CardNumber { get; set; } = string.Empty;
}
