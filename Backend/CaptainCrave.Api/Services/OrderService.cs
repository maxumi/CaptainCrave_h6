using Api.DTOs;
using Api.Mappers;
using Api.Models;
using Api.Models.Enums;
using Api.Repositories;

namespace Api.Services;

// Håndterer forretningslogikken for ordre-operationer.
public class OrderService(
    IOrderRepository orderRepository,
    IUserRepository userRepository,
    IRestaurantRepository restaurantRepository,
    IMenuItemRepository menuItemRepository,
    INotificationService notificationService) : IOrderService
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;
    private readonly IMenuItemRepository _menuItemRepository = menuItemRepository;
    private readonly INotificationService _notificationService = notificationService;

    /// <summary>
    /// Henter en ordre ud fra dens id.
    /// </summary>
    /// <returns>Ordren som DTO, eller null hvis den ikke findes.</returns>
    public async Task<OrderDto?> GetByIdAsync(int id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        return order?.ToDto();
    }

    /// <summary>
    /// Opretter en ny ordre: tjekker at bruger, restaurant og alle retter rent faktisk findes,
    /// og regner selv totalprisen ud på serveren ud fra retternes rigtige priser i databasen,
    /// ALDRIG ud fra en pris, klienten selv har sendt (så kan man ikke snyde med priserne).
    /// </summary>
    /// <returns>Den nyoprettede ordre som DTO, med status "afventer betaling".</returns>
    public async Task<OrderDto> CreateAsync(CreateOrderDto dto)
    {
        var user = await _userRepository.GetByIdAsync(dto.UserId)
            ?? throw new KeyNotFoundException($"User {dto.UserId} not found.");

        var restaurant = await _restaurantRepository.GetByIdAsync(dto.RestaurantId)
            ?? throw new KeyNotFoundException($"Restaurant {dto.RestaurantId} not found.");

        var order = new Order
        {
            UserId = user.Id,
            RestaurantId = restaurant.Id,
            DeliveryType = dto.DeliveryType,
            DeliveryAddress = dto.DeliveryAddress,
            // Ordren afventer betaling, indtil POST /api/payments gennemfører den falske betaling.
            Status = OrderStatus.AwaitingPayment,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        decimal total = 0;

        foreach (var itemDto in dto.Items)
        {
            var menuItem = await _menuItemRepository.GetByIdAsync(itemDto.MenuItemId)
                ?? throw new KeyNotFoundException($"Menu item {itemDto.MenuItemId} not found.");

            var orderItem = itemDto.ToOrderItem(menuItem.Price);
            total += menuItem.Price * itemDto.Quantity;
            order.OrderItems.Add(orderItem);
        }

        order.TotalPrice = total;

        var created = await _orderRepository.CreateAsync(order);

        // Fylder User/Restaurant ind manuelt, så mapperen kan læse navnene uden endnu et databasekald.
        created.User = user;
        created.Restaurant = restaurant;

        // Restauranten får først besked om ordren, når betalingen er gennemført (se PaymentService).

        return created.ToDto();
    }

    /// <summary>
    /// Finder den restaurant, den nuværende bruger ejer, og henter dens ordrer, der stadig er i gang
    /// (ikke leveret eller annulleret endnu).
    /// </summary>
    /// <returns>Restaurantens igangværende ordrer.</returns>
    public async Task<IEnumerable<OrderDto>> GetRestaurantActiveOrdersAsync(int currentUserId, UserRole currentUserRole)
    {
        var restaurantId = await ResolveRestaurantIdForUserAsync(currentUserId, currentUserRole);
        var orders = await _orderRepository.GetActiveByRestaurantAsync(restaurantId);
        return orders.Select(order => order.ToDto());
    }

    /// <summary>
    /// Finder den restaurant, den nuværende bruger ejer, og henter dens færdige ordrer
    /// (dem der er blevet Delivered eller Cancelled).
    /// </summary>
    /// <returns>Restaurantens tidligere (færdige) ordrer.</returns>
    public async Task<IEnumerable<OrderDto>> GetRestaurantHistoricOrdersAsync(int currentUserId, UserRole currentUserRole)
    {
        var restaurantId = await ResolveRestaurantIdForUserAsync(currentUserId, currentUserRole);
        var orders = await _orderRepository.GetHistoryByRestaurantAsync(restaurantId);
        return orders.Select(order => order.ToDto());
    }

    /// <summary>
    /// Skifter en ordres status (fx fra "under tilberedning" til "på vej"), men kun hvis
    /// brugeren må det, og kun hvis skiftet giver mening (man kan ikke hoppe frem og tilbage
    /// som man vil). Sender en live SignalR-besked til kunden, hvis det lykkes.
    /// </summary>
    /// <returns>True hvis statussen blev opdateret, false hvis ordren ikke findes.</returns>
    public async Task<bool> UpdateStatusAsync(int id, UpdateOrderStatusDto dto, int currentUserId, UserRole currentUserRole)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order is null)
            return false;

        if (currentUserRole == UserRole.Restaurant)
        {
            var restaurant = await _restaurantRepository.GetSingleByUserIdAsync(currentUserId)
                ?? throw new UnauthorizedAccessException("Restaurant profile not found for current user.");

            if (order.RestaurantId != restaurant.Id)
                throw new UnauthorizedAccessException("You can only update orders for your own restaurant.");
        }

        if (order.Status == OrderStatus.AwaitingPayment)
            throw new InvalidOperationException("Order is awaiting payment and cannot be updated yet.");

        if (IsTerminal(order.Status))
            throw new InvalidOperationException("Historic orders cannot be updated.");

        if (!IsValidTransition(order.DeliveryType, order.Status, dto.Status))
            throw new InvalidOperationException($"Invalid transition from {order.Status} to {dto.Status} for {order.DeliveryType} orders.");

        var success = await _orderRepository.UpdateStatusAsync(id, dto.Status);

        // Giv kunden live besked om den nye status (SignalR), men kun hvis opdateringen lykkedes.
        if (success)
            await _notificationService.NotifyOrderStatusChangedAsync(order.UserId, order.Id, dto.Status);

        return success;
    }

    /// <summary>
    /// Henter den ordre, en bruger er ved at have i gang lige nu (endnu ikke leveret/annulleret).
    /// </summary>
    /// <returns>Den aktive ordre som DTO, eller null hvis brugeren ikke har en aktiv ordre.</returns>
    public async Task<OrderDto?> GetActiveOrderForUserAsync(int userId)
    {
        var order = await _orderRepository.GetActiveOrderForUserAsync(userId);
        return order?.ToDto();
    }

    /// <summary>
    /// Henter alle færdige ordrer (Delivered/Cancelled) for en bruger, med den nyeste først.
    /// </summary>
    /// <returns>Brugerens tidligere ordrer.</returns>
    public async Task<IEnumerable<OrderDto>> GetHistoricOrdersForUserAsync(int userId)
    {
        var orders = await _orderRepository.GetHistoryForUserAsync(userId);
        return orders.Select(order => order.ToDto());
    }

    /// <summary>
    /// Bruges til at afgøre om en kunde må skrive en anmeldelse: kun kunder, der rent faktisk
    /// har fået en ordre leveret fra restauranten, må anmelde den (man kan ikke anmelde et sted,
    /// man aldrig har handlet hos).
    /// </summary>
    /// <returns>True hvis kunden har fået mindst én ordre leveret fra restauranten.</returns>
    public Task<bool> HasCustomerOrderedFromRestaurantAsync(int userId, int restaurantId) =>
        _orderRepository.HasUserOrderedFromRestaurantAsync(userId, restaurantId);

    /// <summary>
    /// Slår restauranten op, som den nuværende bruger ejer, og giver dens id tilbage.
    /// Kaster en fejl, hvis brugeren er Admin (admin skal bruge en anden metode) eller slet
    /// ikke har en restaurant.
    /// </summary>
    /// <returns>Id'et på restauranten, brugeren ejer.</returns>
    private async Task<int> ResolveRestaurantIdForUserAsync(int currentUserId, UserRole currentUserRole)
    {
        if (currentUserRole == UserRole.Admin)
            throw new InvalidOperationException("Admin must use explicit admin reporting endpoints.");

        var restaurant = await _restaurantRepository.GetSingleByUserIdAsync(currentUserId)
            ?? throw new KeyNotFoundException("Restaurant profile not found for current user.");

        return restaurant.Id;
    }

    /// <summary>
    /// Tjekker om en ordre har nået en "færdig" status (Delivered eller Cancelled),
    /// som den ikke kan ændres fra igen, ligesom en afsluttet sag.
    /// </summary>
    /// <returns>True hvis ordren er færdig og låst.</returns>
    private static bool IsTerminal(OrderStatus status) =>
        status == OrderStatus.Delivered || status == OrderStatus.Cancelled;

    /// <summary>
    /// Tjekker om en ordre må skifte fra sin nuværende status til den ønskede næste status.
    /// Den lovlige rækkefølge af statusser er forskellig, alt efter om det er levering eller afhentning.
    /// </summary>
    /// <returns>True hvis skiftet er lovligt.</returns>
    private static bool IsValidTransition(DeliveryType deliveryType, OrderStatus currentStatus, OrderStatus nextStatus)
    {
        // Afvis skift der ikke ændrer noget, statussen skal rent faktisk blive anderledes.
        if (currentStatus == nextStatus)
            return false;

        // Man må altid annullere, medmindre ordren allerede er blevet leveret.
        if (nextStatus == OrderStatus.Cancelled)
            return currentStatus != OrderStatus.Delivered;

        // Håndhæver den lige rækkefølge af statusser, alt efter leveringstype:
        return deliveryType switch
        {
            DeliveryType.Delivery => currentStatus switch
            {
                OrderStatus.Pending => nextStatus == OrderStatus.Preparing,
                OrderStatus.Preparing => nextStatus == OrderStatus.OnTheWay,
                OrderStatus.OnTheWay => nextStatus == OrderStatus.Delivered,
                _ => false
            },
            DeliveryType.Pickup => currentStatus switch
            {
                OrderStatus.Pending => nextStatus == OrderStatus.Preparing,
                OrderStatus.Preparing => nextStatus == OrderStatus.ReadyForPickup,
                OrderStatus.ReadyForPickup => nextStatus == OrderStatus.Delivered,
                _ => false
            },
            _ => false
        };
    }

    /// <summary>
    /// Henter en restaurants igangværende ordrer, når restaurant-id'et allerede kendes
    /// (bruges af admin-visninger, der spørger på vegne af en bestemt restaurant).
    /// </summary>
    /// <returns>Restaurantens igangværende ordrer.</returns>
    public async Task<IEnumerable<OrderDto>> GetRestaurantActiveOrdersByRestaurantIdAsync(int restaurantId)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId)
            ?? throw new KeyNotFoundException($"Restaurant {restaurantId} not found.");

        var orders = await _orderRepository.GetActiveByRestaurantAsync(restaurant.Id);

        return orders.Select(order => order.ToDto());
    }

    /// <summary>
    /// Henter en restaurants færdige ordrer, når restaurant-id'et allerede kendes
    /// (bruges af admin-visninger, der spørger på vegne af en bestemt restaurant).
    /// </summary>
    /// <returns>Restaurantens tidligere (færdige) ordrer.</returns>
    public async Task<IEnumerable<OrderDto>> GetRestaurantHistoricOrdersByRestaurantIdAsync(int restaurantId)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId)
            ?? throw new KeyNotFoundException($"Restaurant {restaurantId} not found.");

        var orders = await _orderRepository.GetHistoryByRestaurantAsync(restaurant.Id);

        return orders.Select(order => order.ToDto());
    }
}
