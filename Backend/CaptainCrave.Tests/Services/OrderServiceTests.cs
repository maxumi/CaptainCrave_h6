using Api.DTOs;
using Api.Models;
using Api.Models.Enums;
using Api.Repositories;
using Api.Services;
using Moq;

namespace Api.Tests.Services;

// Unit tests for OrderService business logic (status transitions, ownership checks, totals).
// All repositories and the notification service are mocked so no database or SignalR calls occur.
public class OrderServiceTests
{
    private static (
        OrderService service,
        Mock<IOrderRepository> mockOrderRepository,
        Mock<IUserRepository> mockUserRepository,
        Mock<IRestaurantRepository> mockRestaurantRepository,
        Mock<IMenuItemRepository> mockMenuItemRepository,
        Mock<INotificationService> mockNotificationService) CreateService()
    {
        var mockOrderRepository = new Mock<IOrderRepository>();
        var mockUserRepository = new Mock<IUserRepository>();
        var mockRestaurantRepository = new Mock<IRestaurantRepository>();
        var mockMenuItemRepository = new Mock<IMenuItemRepository>();
        var mockNotificationService = new Mock<INotificationService>();

        var service = new OrderService(
            mockOrderRepository.Object,
            mockUserRepository.Object,
            mockRestaurantRepository.Object,
            mockMenuItemRepository.Object,
            mockNotificationService.Object);

        return (service, mockOrderRepository, mockUserRepository, mockRestaurantRepository, mockMenuItemRepository, mockNotificationService);
    }

    // CreateAsync

    [Fact]
    public async Task CreateAsync_ValidDto_CalculatesTotalFromMenuItemPrices()
    {
        var (service, mockOrderRepository, mockUserRepository, mockRestaurantRepository, mockMenuItemRepository, _) = CreateService();

        mockUserRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1, Name = "Alice", Email = "alice@test.dk" });
        mockRestaurantRepository.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Restaurant { Id = 2, Name = "Burger Place", UserId = 5 });
        mockMenuItemRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new MenuItem { Id = 10, Name = "Burger", Price = 50m });
        mockMenuItemRepository.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(new MenuItem { Id = 11, Name = "Fries", Price = 20m });
        mockOrderRepository.Setup(r => r.CreateAsync(It.IsAny<Order>())).ReturnsAsync((Order o) => { o.Id = 100; return o; });

        var dto = new CreateOrderDto
        {
            UserId = 1,
            RestaurantId = 2,
            DeliveryType = DeliveryType.Pickup,
            Items =
            [
                new CreateOrderItemDto { MenuItemId = 10, Quantity = 2 },
                new CreateOrderItemDto { MenuItemId = 11, Quantity = 1 }
            ]
        };

        var result = await service.CreateAsync(dto);

        // 2 * 50 + 1 * 20 = 120
        Assert.Equal(120m, result.TotalPrice);
    }

    [Fact]
    public async Task CreateAsync_UnknownUser_ThrowsKeyNotFoundException()
    {
        var (service, _, mockUserRepository, _, _, _) = CreateService();
        mockUserRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        var dto = new CreateOrderDto { UserId = 99, RestaurantId = 1, DeliveryType = DeliveryType.Pickup, Items = [new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 }] };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_UnknownRestaurant_ThrowsKeyNotFoundException()
    {
        var (service, _, mockUserRepository, mockRestaurantRepository, _, _) = CreateService();
        mockUserRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1, Name = "Alice", Email = "alice@test.dk" });
        mockRestaurantRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Restaurant?)null);

        var dto = new CreateOrderDto { UserId = 1, RestaurantId = 99, DeliveryType = DeliveryType.Pickup, Items = [new CreateOrderItemDto { MenuItemId = 1, Quantity = 1 }] };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_UnknownMenuItem_ThrowsKeyNotFoundException()
    {
        var (service, _, mockUserRepository, mockRestaurantRepository, mockMenuItemRepository, _) = CreateService();
        mockUserRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1, Name = "Alice", Email = "alice@test.dk" });
        mockRestaurantRepository.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Restaurant { Id = 2, Name = "Burger Place", UserId = 5 });
        mockMenuItemRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((MenuItem?)null);

        var dto = new CreateOrderDto { UserId = 1, RestaurantId = 2, DeliveryType = DeliveryType.Pickup, Items = [new CreateOrderItemDto { MenuItemId = 99, Quantity = 1 }] };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_ValidDto_DoesNotSendNewOrderNotification()
    {
        // Restauranten skal først have besked, når betalingen er gennemført (se PaymentService), ikke ved oprettelse.
        var (service, mockOrderRepository, mockUserRepository, mockRestaurantRepository, mockMenuItemRepository, mockNotificationService) = CreateService();
        mockUserRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1, Name = "Alice", Email = "alice@test.dk" });
        mockRestaurantRepository.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Restaurant { Id = 2, Name = "Burger Place", UserId = 5 });
        mockMenuItemRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new MenuItem { Id = 10, Name = "Burger", Price = 50m });
        mockOrderRepository.Setup(r => r.CreateAsync(It.IsAny<Order>())).ReturnsAsync((Order o) => { o.Id = 100; return o; });

        var dto = new CreateOrderDto { UserId = 1, RestaurantId = 2, DeliveryType = DeliveryType.Pickup, Items = [new CreateOrderItemDto { MenuItemId = 10, Quantity = 1 }] };

        var result = await service.CreateAsync(dto);

        Assert.Equal(OrderStatus.AwaitingPayment, result.Status);
        mockNotificationService.Verify(n => n.NotifyNewOrderAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    // UpdateStatusAsync

    [Fact]
    public async Task UpdateStatusAsync_OrderNotFound_ReturnsFalse()
    {
        var (service, mockOrderRepository, _, _, _, _) = CreateService();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Order?)null);

        var result = await service.UpdateStatusAsync(1, new UpdateOrderStatusDto { Status = OrderStatus.Preparing }, 1, UserRole.Restaurant);

        Assert.False(result);
    }

    [Theory]
    [InlineData(DeliveryType.Pickup, OrderStatus.Pending, OrderStatus.Preparing)]
    [InlineData(DeliveryType.Pickup, OrderStatus.Preparing, OrderStatus.ReadyForPickup)]
    [InlineData(DeliveryType.Pickup, OrderStatus.ReadyForPickup, OrderStatus.Delivered)]
    [InlineData(DeliveryType.Delivery, OrderStatus.Pending, OrderStatus.Preparing)]
    [InlineData(DeliveryType.Delivery, OrderStatus.Preparing, OrderStatus.OnTheWay)]
    [InlineData(DeliveryType.Delivery, OrderStatus.OnTheWay, OrderStatus.Delivered)]
    public async Task UpdateStatusAsync_ValidTransition_ReturnsTrue(DeliveryType deliveryType, OrderStatus from, OrderStatus to)
    {
        var (service, mockOrderRepository, _, mockRestaurantRepository, _, _) = CreateService();
        var order = new Order { Id = 1, RestaurantId = 2, UserId = 3, Status = from, DeliveryType = deliveryType };
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        mockRestaurantRepository.Setup(r => r.GetSingleByUserIdAsync(9)).ReturnsAsync(new Restaurant { Id = 2, UserId = 9 });
        mockOrderRepository.Setup(r => r.UpdateStatusAsync(1, to)).ReturnsAsync(true);

        var result = await service.UpdateStatusAsync(1, new UpdateOrderStatusDto { Status = to }, 9, UserRole.Restaurant);

        Assert.True(result);
    }

    [Fact]
    public async Task UpdateStatusAsync_SkippingAStep_ThrowsInvalidOperationException()
    {
        var (service, mockOrderRepository, _, mockRestaurantRepository, _, _) = CreateService();
        var order = new Order { Id = 1, RestaurantId = 2, UserId = 3, Status = OrderStatus.Pending, DeliveryType = DeliveryType.Pickup };
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        mockRestaurantRepository.Setup(r => r.GetSingleByUserIdAsync(9)).ReturnsAsync(new Restaurant { Id = 2, UserId = 9 });

        // Pending -> Delivered skips Preparing/ReadyForPickup, which is not a valid transition.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateStatusAsync(1, new UpdateOrderStatusDto { Status = OrderStatus.Delivered }, 9, UserRole.Restaurant));
    }

    [Fact]
    public async Task UpdateStatusAsync_AlreadyDelivered_ThrowsInvalidOperationException()
    {
        var (service, mockOrderRepository, _, mockRestaurantRepository, _, _) = CreateService();
        var order = new Order { Id = 1, RestaurantId = 2, UserId = 3, Status = OrderStatus.Delivered, DeliveryType = DeliveryType.Pickup };
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        mockRestaurantRepository.Setup(r => r.GetSingleByUserIdAsync(9)).ReturnsAsync(new Restaurant { Id = 2, UserId = 9 });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateStatusAsync(1, new UpdateOrderStatusDto { Status = OrderStatus.Cancelled }, 9, UserRole.Restaurant));
    }

    [Fact]
    public async Task UpdateStatusAsync_CancelFromPending_ReturnsTrue()
    {
        var (service, mockOrderRepository, _, mockRestaurantRepository, _, _) = CreateService();
        var order = new Order { Id = 1, RestaurantId = 2, UserId = 3, Status = OrderStatus.Pending, DeliveryType = DeliveryType.Pickup };
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        mockRestaurantRepository.Setup(r => r.GetSingleByUserIdAsync(9)).ReturnsAsync(new Restaurant { Id = 2, UserId = 9 });
        mockOrderRepository.Setup(r => r.UpdateStatusAsync(1, OrderStatus.Cancelled)).ReturnsAsync(true);

        var result = await service.UpdateStatusAsync(1, new UpdateOrderStatusDto { Status = OrderStatus.Cancelled }, 9, UserRole.Restaurant);

        Assert.True(result);
    }

    [Fact]
    public async Task UpdateStatusAsync_RestaurantUserDoesNotOwnOrder_ThrowsUnauthorizedAccessException()
    {
        var (service, mockOrderRepository, _, mockRestaurantRepository, _, _) = CreateService();
        var order = new Order { Id = 1, RestaurantId = 2, UserId = 3, Status = OrderStatus.Pending, DeliveryType = DeliveryType.Pickup };
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        // Restaurant owned by this caller has a different id (99) than the order's restaurant (2).
        mockRestaurantRepository.Setup(r => r.GetSingleByUserIdAsync(9)).ReturnsAsync(new Restaurant { Id = 99, UserId = 9 });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateStatusAsync(1, new UpdateOrderStatusDto { Status = OrderStatus.Preparing }, 9, UserRole.Restaurant));
    }

    [Fact]
    public async Task UpdateStatusAsync_ValidTransition_SendsStatusChangedNotification()
    {
        var (service, mockOrderRepository, _, mockRestaurantRepository, _, mockNotificationService) = CreateService();
        var order = new Order { Id = 1, RestaurantId = 2, UserId = 3, Status = OrderStatus.Pending, DeliveryType = DeliveryType.Pickup };
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        mockRestaurantRepository.Setup(r => r.GetSingleByUserIdAsync(9)).ReturnsAsync(new Restaurant { Id = 2, UserId = 9 });
        mockOrderRepository.Setup(r => r.UpdateStatusAsync(1, OrderStatus.Preparing)).ReturnsAsync(true);

        await service.UpdateStatusAsync(1, new UpdateOrderStatusDto { Status = OrderStatus.Preparing }, 9, UserRole.Restaurant);

        mockNotificationService.Verify(n => n.NotifyOrderStatusChangedAsync(3, 1, OrderStatus.Preparing), Times.Once);
    }

    // Restaurant-scoped queries

    [Fact]
    public async Task GetRestaurantActiveOrdersAsync_AdminRole_ThrowsInvalidOperationException()
    {
        var (service, _, _, _, _, _) = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetRestaurantActiveOrdersAsync(1, UserRole.Admin));
    }

    [Fact]
    public async Task GetRestaurantActiveOrdersAsync_NoRestaurantProfile_ThrowsKeyNotFoundException()
    {
        var (service, _, _, mockRestaurantRepository, _, _) = CreateService();
        mockRestaurantRepository.Setup(r => r.GetSingleByUserIdAsync(1)).ReturnsAsync((Restaurant?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetRestaurantActiveOrdersAsync(1, UserRole.Restaurant));
    }

    [Fact]
    public async Task GetActiveOrderForUserAsync_ReturnsMappedOrder()
    {
        var (service, mockOrderRepository, _, _, _, _) = CreateService();
        mockOrderRepository.Setup(r => r.GetActiveOrderForUserAsync(1)).ReturnsAsync(new Order { Id = 5, UserId = 1, RestaurantId = 2 });

        var result = await service.GetActiveOrderForUserAsync(1);

        Assert.Equal(5, result?.Id);
    }

    [Fact]
    public async Task HasCustomerOrderedFromRestaurantAsync_DelegatesToRepository()
    {
        var (service, mockOrderRepository, _, _, _, _) = CreateService();
        mockOrderRepository.Setup(r => r.HasUserOrderedFromRestaurantAsync(1, 2)).ReturnsAsync(true);

        var result = await service.HasCustomerOrderedFromRestaurantAsync(1, 2);

        Assert.True(result);
    }
}
