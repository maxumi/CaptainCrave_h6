namespace Api.Hubs;

// Bygger konsistente SignalR-gruppenavne, så både Hub'en og notifikationsservicen
// er enige om, hvem der skal modtage en besked.
public static class NotificationGroups
{
    // Gruppen for alle aktive forbindelser for en bestemt bruger.
    // Returnerer gruppenavnet, som denne bruger skal lægges i.
    public static string User(int userId) => $"user-{userId}";

    // Gruppen for alle aktive forbindelser, der tilhører en restaurants ejer.
    // Returnerer gruppenavnet, som restaurantens ejer skal lægges i.
    public static string Restaurant(int restaurantId) => $"restaurant-{restaurantId}";
}
