namespace Api.Models.Enums;

// Beskriver hvordan en ordre skal nå frem til kunden: bragt ud eller hentet selv.
public enum DeliveryType
{
    // Ordren bliver kørt ud til kundens adresse.
    Delivery,

    // Kunden henter selv ordren hos restauranten.
    Pickup
}
