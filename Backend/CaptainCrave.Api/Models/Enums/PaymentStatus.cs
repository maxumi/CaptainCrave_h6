namespace Api.Models.Enums;

// Status for et (falsk) betalingsforsøg på en ordre.
public enum PaymentStatus
{
    // Betalingen er oprettet, men endnu ikke behandlet af den falske gateway.
    Pending,

    // Betalingen blev gennemført.
    Succeeded,

    // Betalingen blev afvist (fx falsk "kort afvist").
    Failed,

    // Betalingen blev annulleret, inden den blev gennemført.
    Cancelled,

    // Betalingen er senere blevet refunderet.
    Refunded
}
