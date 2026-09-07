namespace Api.Hubs;

// Bygger konsistente SignalR-gruppenavne, så både Hub'en og notifikationsservicen
// er enige om, hvem der skal modtage en besked.
public static class NotificationGroups
{
    /// <summary>Gruppen for alle aktive forbindelser for én bestemt bruger.</summary>
    /// <returns>Gruppenavnet, som denne bruger skal lægges i.</returns>
    public static string User(int userId) => $"user-{userId}";

    /// <summary>Gruppen for alle aktive forbindelser, der tilhører en restaurants ejer.</summary>
    /// <returns>Gruppenavnet, som restaurantens ejer skal lægges i.</returns>
    public static string Restaurant(int restaurantId) => $"restaurant-{restaurantId}";
}
