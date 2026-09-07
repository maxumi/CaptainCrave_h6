using Api.Models.Enums;

namespace Api.Services;

/// <summary>
/// Definerer live (real-time) beskeder der sendes til forbundne klienter via SignalR.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Giver restaurantens forbundne klienter besked om en ny ordre.
    /// </summary>
    /// <param name="restaurantId">ID på restauranten der modtog ordren.</param>
    /// <param name="orderId">ID på den nye ordre.</param>
    Task NotifyNewOrderAsync(int restaurantId, int orderId);

    /// <summary>
    /// Giver kundens forbundne klienter besked om at ordrens status er ændret.
    /// </summary>
    /// <param name="userId">ID på kunden der har afgivet ordren.</param>
    /// <param name="orderId">ID på ordren hvis status er ændret.</param>
    /// <param name="newStatus">ID på den nye status.</param>
    Task NotifyOrderStatusChangedAsync(int userId, int orderId, OrderStatus newStatus);
}