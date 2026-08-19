using Api.Hubs;
using Api.Models.Enums;
using Microsoft.AspNetCore.SignalR;

namespace Api.Services;

/// <summary>
/// Sender ordre-notifikationer til forbundne klienter gennem NotificationHub.
/// </summary>
public class SignalRNotificationService(IHubContext<NotificationHub> hubContext) : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext = hubContext;

    /// <summary>Sender "NewOrder"-event til alle forbindelser i restaurantens gruppe.</summary>
    /// <param name="restaurantId">Restauranten der skal have besked.</param>
    /// <param name="orderId">Id på den nye ordre.</param>
    public Task NotifyNewOrderAsync(int restaurantId, int orderId) =>
        _hubContext.Clients.Group(NotificationGroups.Restaurant(restaurantId))
            .SendAsync("NewOrder", new { orderId });

    /// <summary>Sender "OrderStatusChanged"-event til alle forbindelser i kundens gruppe.</summary>
    /// <param name="userId">Kunden der skal have besked.</param>
    /// <param name="orderId">Ordren hvis status er ændret.</param>
    /// <param name="newStatus">Ordrens nye status.</param>
    public Task NotifyOrderStatusChangedAsync(int userId, int orderId, OrderStatus newStatus) =>
        _hubContext.Clients.Group(NotificationGroups.User(userId))
            .SendAsync("OrderStatusChanged", new { orderId, status = newStatus.ToString() });
}