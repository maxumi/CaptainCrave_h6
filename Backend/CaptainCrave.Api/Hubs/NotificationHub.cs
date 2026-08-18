using Api.Models.Enums;
using Api.Repositories;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;

/// <summary>
/// SignalR-hub som klienter forbinder til for at modtage live besked om ordrer.
/// Kræver et gyldigt JWT. Når en klient forbinder, bliver den automatisk lagt
/// i sin egen gruppe, og — hvis brugeren er en restaurant — også restaurantens gruppe.
/// </summary>
[Authorize]
public class NotificationHub(IRestaurantRepository restaurantRepository) : Hub
{
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;

    /// <summary>
    /// Lægger den forbindende klient i de rigtige grupper, så den kun modtager
    /// beskeder der er relevante for netop den bruger.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        // Alle brugere lægges i deres egen personlige gruppe (bruges til f.eks. "din ordrestatus ændrede sig").
        var userId = Context.User!.GetId();
        await Groups.AddToGroupAsync(Context.ConnectionId, NotificationGroups.User(userId));

        // Restaurantejere lægges også i deres restaurants gruppe (bruges til "ny ordre modtaget").
        if (Context.User!.GetRole() == UserRole.Restaurant)
        {
            var restaurant = await _restaurantRepository.GetSingleByUserIdAsync(userId);
            if (restaurant is not null)
                await Groups.AddToGroupAsync(Context.ConnectionId, NotificationGroups.Restaurant(restaurant.Id));
        }

        await base.OnConnectedAsync();
    }
}