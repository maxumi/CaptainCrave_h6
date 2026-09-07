namespace Api.Models.Enums;

// Alle de statusser en ordre kan have, fra den bliver oprettet til den er færdig.
public enum OrderStatus
{
    // Ordren er lagt og betalt, men restauranten er ikke gået i gang endnu.
    Pending,

    // Restauranten er i gang med at lave maden.
    Preparing,

    // Ordren er på vej ud til kunden (kun ved levering).
    OnTheWay,

    // Ordren står klar til at blive hentet (kun ved afhentning).
    ReadyForPickup,

    // Ordren er færdig, leveret eller afhentet.
    Delivered,

    // Ordren er annulleret og bliver ikke færdiggørt.
    Cancelled,

    // Ordren er oprettet, men den falske betaling er endnu ikke gennemført (se PaymentsController).
    // Ligger sidst i enummet (ikke 0), så den ikke bliver forvekslet med CLR-standardværdien af EF Core.
    AwaitingPayment
}
