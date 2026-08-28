namespace Api.Models.Enums;

// Represents the possible statuses an order can have.
public enum OrderStatus
{
    // The order has been placed.
    Pending,

    // The restaurant is preparing the order.
    Preparing,

    // The order is being delivered.
    OnTheWay,

    // The order is ready for pick up.
    ReadyForPickup,

    // The order has been completed and delivered or picked up.
    Delivered,

    // The order has been cancelled and will not be completed.
    Cancelled,

    // Ordren er oprettet, men den falske betaling er endnu ikke gennemført (se PaymentsController).
    // Ligger sidst i enummet (ikke 0), så den ikke bliver forvekslet med CLR-standardværdien af EF Core.
    AwaitingPayment
}