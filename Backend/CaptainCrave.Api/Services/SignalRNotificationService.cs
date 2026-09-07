using Api.Hubs;
using Api.Models.Enums;
using Microsoft.AspNetCore.SignalR;

namespace Api.Services;

// Sender live-beskeder (notifikationer) om ordrer ud til de klienter, der lige nu er
// forbundet via SignalR, gennem NotificationHub.
public class SignalRNotificationService(IHubContext<NotificationHub> hubContext) : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext = hubContext;

    // Sender en "NewOrder"-besked til alle, der lytter i restaurantens gruppe,
    // så restauranten kan se den nye ordre dukke op med det samme, uden at genindlæse siden.
    public Task NotifyNewOrderAsync(int restaurantId, int orderId) =>
        _hubContext.Clients.Group(NotificationGroups.Restaurant(restaurantId))
            .SendAsync("NewOrder", new { orderId });

    // Sender en "OrderStatusChanged"-besked til kunden, så personen kan se med det samme,
    // når restauranten f.eks. skifter ordren til "under tilberedning" eller "leveret".
    public Task NotifyOrderStatusChangedAsync(int userId, int orderId, OrderStatus newStatus) =>
        _hubContext.Clients.Group(NotificationGroups.User(userId))
            .SendAsync("OrderStatusChanged", new { orderId, status = newStatus.ToString() });
}
