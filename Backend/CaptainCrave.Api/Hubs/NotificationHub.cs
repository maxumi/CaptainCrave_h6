using Api.Models.Enums;
using Api.Repositories;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;

// Klienten connecter til denne hub for at modtage live notifikationer.
// kræver et gyldigt JWT 

// Arver fra SignalR's Hub-klasse som håndterer realtidskommunikation mellem server og klient. 
[Authorize]
public class NotificationHub(IRestaurantRepository restaurantRepository) : Hub
{
    // DI: gemmer dependency i en private readonly field.
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;

    // Kører automatisk, når en klient connecter til hubben, og overrider base-metoden [Hub.OnConnectedAsync()]
    public override async Task OnConnectedAsync()
    {
        // Placere brugeren i sin egen personlige gruppe, så der ikke sendes notifikationer til andre brugere.
        var userId = Context.User!.GetId();
        await Groups.AddToGroupAsync(Context.ConnectionId, NotificationGroups.User(userId));

        // Hvis brugeren er en restaurantejer, placeres de også i deres restaurants gruppe.
        if (Context.User!.GetRole() == UserRole.Restaurant)
        {
            var restaurant = await _restaurantRepository.GetSingleByUserIdAsync(userId);
            if (restaurant is not null)
                await Groups.AddToGroupAsync(Context.ConnectionId, NotificationGroups.Restaurant(restaurant.Id));
        }

        // base-metoden skal altid kaldes til sidst for at sikre korrekt SignalR-opførsel.
        await base.OnConnectedAsync();
    }
}
