using Api.Hubs;
using Api.Models.Enums;
using Microsoft.AspNetCore.SignalR;

namespace Api.Services;

// Sender notifikationer, om ordrer ud til de klienter, der lige nu er
// forbundet via SignalR, gennem NotificationHub.

// IHubContext injected for at give klassen evne til at sende beskeder ud 
// til de klienter der er forbundet til Hub'en, uden at være i huben
public class SignalRNotificationService(IHubContext<NotificationHub> hubContext) : INotificationService
{
    // DI: gemmer i en private readonly field.
    private readonly IHubContext<NotificationHub> _hubContext = hubContext;

    // Sender "NewOrder" til restauranten, når der kommer en ny ordre fra en kunde.
    public Task NotifyNewOrderAsync(int restaurantId, int orderId) =>
        
        // Kun restaurantens egen gruppe modtager beskeden; "NewOrder" er eventnavnet klienten lytter efter.
        _hubContext.Clients.Group(NotificationGroups.Restaurant(restaurantId))
            .SendAsync("NewOrder", new { orderId });

    // Sender en "OrderStatusChanged"-besked til kunden, så personen kan se med det samme,
    // når restauranten f.eks. skifter ordren til "under tilberedning" eller "leveret".
    public Task NotifyOrderStatusChangedAsync(int userId, int orderId, OrderStatus newStatus) =>
        _hubContext.Clients.Group(NotificationGroups.User(userId))
            .SendAsync("OrderStatusChanged", new { orderId, status = newStatus.ToString() });
}
