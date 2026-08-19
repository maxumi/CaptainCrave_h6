namespace Api.Hubs;

/// <summary>
/// Bygger konsistente SignalR-gruppenavne, så både Hub'en og notifikationsservicen
/// er enige om, hvem der skal modtage en besked.
/// </summary>
public static class NotificationGroups
{
    /// <summary>Gruppen for alle aktive forbindelser for én bestemt bruger.</summary>
    /// <param name="userId">Brugerens id (kunde eller restaurantejer).</param>
    public static string User(int userId) => $"user-{userId}";

    /// <summary>Gruppen for alle aktive forbindelser der tilhører en restaurants ejer.</summary>
    /// <param name="restaurantId">Restaurantens id.</param>
    public static string Restaurant(int restaurantId) => $"restaurant-{restaurantId}";
}