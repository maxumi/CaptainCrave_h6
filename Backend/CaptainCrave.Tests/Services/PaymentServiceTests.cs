using Api.DTOs;
using Api.Models;
using Api.Models.Enums;
using Api.Repositories;
using Api.Services;
using Moq;

namespace Api.Tests.Services;

// Unit tests for PaymentService (the fake/mock payment gateway).
// IPaymentRepository, IOrderRepository, and INotificationService are all mocked, so no
// database or SignalR calls occur — only the gateway/business logic itself is exercised.
public class PaymentServiceTests
{
    private static (
        PaymentService service,
        Mock<IPaymentRepository> mockPaymentRepository,
        Mock<IOrderRepository> mockOrderRepository,
        Mock<INotificationService> mockNotificationService) CreateService()
    {
        var mockPaymentRepository = new Mock<IPaymentRepository>();
        var mockOrderRepository = new Mock<IOrderRepository>();
        var mockNotificationService = new Mock<INotificationService>();

        var service = new PaymentService(
            mockPaymentRepository.Object,
            mockOrderRepository.Object,
            mockNotificationService.Object);

        return (service, mockPaymentRepository, mockOrderRepository, mockNotificationService);
    }

    private static Order MakeOrder(int id = 1, OrderStatus status = OrderStatus.AwaitingPayment) => new()
    {
        Id = id,
        UserId = 10,
        RestaurantId = 5,
        Status = status,
        TotalPrice = 99.50m
    };

    // ProcessPaymentAsync

    [Fact]
    public async Task ProcessPaymentAsync_UnknownOrder_ThrowsKeyNotFoundException()
    {
        var (service, _, mockOrderRepository, _) = CreateService();
        mockOrderRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Order?)null);

        var dto = new CreatePaymentDto { OrderId = 99, CardNumber = "4111111111111111" };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ProcessPaymentAsync(dto));
    }

    [Fact]
    public async Task ProcessPaymentAsync_OrderNotAwaitingPayment_ThrowsInvalidOperationException()
    {
        var (service, _, mockOrderRepository, _) = CreateService();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeOrder(status: OrderStatus.Pending));

        var dto = new CreatePaymentDto { OrderId = 1, CardNumber = "4111111111111111" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessPaymentAsync(dto));
    }

    [Fact]
    public async Task ProcessPaymentAsync_CardNotEndingInZeros_SucceedsAndMarksPaymentSucceeded()
    {
        var (service, mockPaymentRepository, mockOrderRepository, _) = CreateService();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeOrder());
        mockPaymentRepository.Setup(r => r.CreateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => { p.Id = 1; return p; });

        var dto = new CreatePaymentDto { OrderId = 1, CardNumber = "4111111111111111" };

        var result = await service.ProcessPaymentAsync(dto);

        Assert.Equal(PaymentStatus.Succeeded, result.Status);
    }

    [Fact]
    public async Task ProcessPaymentAsync_CardEndingInFourZeros_FailsAndMarksPaymentFailed()
    {
        var (service, mockPaymentRepository, mockOrderRepository, _) = CreateService();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeOrder());
        mockPaymentRepository.Setup(r => r.CreateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => { p.Id = 1; return p; });

        var dto = new CreatePaymentDto { OrderId = 1, CardNumber = "4111111110000" };

        var result = await service.ProcessPaymentAsync(dto);

        Assert.Equal(PaymentStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ProcessPaymentAsync_Succeeds_UsesOrderTotalPriceNotClientInput()
    {
        // The amount must come from the order server-side, since CreatePaymentDto has no Amount field at all.
        var (service, mockPaymentRepository, mockOrderRepository, _) = CreateService();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeOrder());
        mockPaymentRepository.Setup(r => r.CreateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => { p.Id = 1; return p; });

        var dto = new CreatePaymentDto { OrderId = 1, CardNumber = "4111111111111111" };

        var result = await service.ProcessPaymentAsync(dto);

        Assert.Equal(99.50m, result.Amount);
    }

    [Fact]
    public async Task ProcessPaymentAsync_Succeeds_SetsProviderReference()
    {
        var (service, mockPaymentRepository, mockOrderRepository, _) = CreateService();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeOrder());
        mockPaymentRepository.Setup(r => r.CreateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => { p.Id = 1; return p; });

        var dto = new CreatePaymentDto { OrderId = 1, CardNumber = "4111111111111111" };

        var result = await service.ProcessPaymentAsync(dto);

        Assert.False(string.IsNullOrWhiteSpace(result.ProviderReference));
    }

    [Fact]
    public async Task ProcessPaymentAsync_Fails_DoesNotSetProviderReference()
    {
        var (service, mockPaymentRepository, mockOrderRepository, _) = CreateService();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeOrder());
        mockPaymentRepository.Setup(r => r.CreateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => { p.Id = 1; return p; });

        var dto = new CreatePaymentDto { OrderId = 1, CardNumber = "4111111110000" };

        var result = await service.ProcessPaymentAsync(dto);

        Assert.Null(result.ProviderReference);
    }

    [Fact]
    public async Task ProcessPaymentAsync_Succeeds_UpdatesOrderStatusToPending()
    {
        var (service, mockPaymentRepository, mockOrderRepository, _) = CreateService();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeOrder());
        mockPaymentRepository.Setup(r => r.CreateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => { p.Id = 1; return p; });

        var dto = new CreatePaymentDto { OrderId = 1, CardNumber = "4111111111111111" };

        await service.ProcessPaymentAsync(dto);

        mockOrderRepository.Verify(r => r.UpdateStatusAsync(1, OrderStatus.Pending), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_Succeeds_SendsNewOrderNotification()
    {
        var (service, mockPaymentRepository, mockOrderRepository, mockNotificationService) = CreateService();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeOrder());
        mockPaymentRepository.Setup(r => r.CreateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => { p.Id = 1; return p; });

        var dto = new CreatePaymentDto { OrderId = 1, CardNumber = "4111111111111111" };

        await service.ProcessPaymentAsync(dto);

        mockNotificationService.Verify(n => n.NotifyNewOrderAsync(5, 1), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_Fails_DoesNotUpdateOrderStatus()
    {
        var (service, mockPaymentRepository, mockOrderRepository, _) = CreateService();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeOrder());
        mockPaymentRepository.Setup(r => r.CreateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => { p.Id = 1; return p; });

        var dto = new CreatePaymentDto { OrderId = 1, CardNumber = "4111111110000" };

        await service.ProcessPaymentAsync(dto);

        mockOrderRepository.Verify(r => r.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<OrderStatus>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_Fails_DoesNotSendNewOrderNotification()
    {
        var (service, mockPaymentRepository, mockOrderRepository, mockNotificationService) = CreateService();
        mockOrderRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeOrder());
        mockPaymentRepository.Setup(r => r.CreateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => { p.Id = 1; return p; });

        var dto = new CreatePaymentDto { OrderId = 1, CardNumber = "4111111110000" };

        await service.ProcessPaymentAsync(dto);

        mockNotificationService.Verify(n => n.NotifyNewOrderAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    // GetLatestByOrderIdAsync

    [Fact]
    public async Task GetLatestByOrderIdAsync_PaymentExists_ReturnsDto()
    {
        var (service, mockPaymentRepository, _, _) = CreateService();
        var payment = new Payment { Id = 1, OrderId = 1, Amount = 50m, Status = PaymentStatus.Succeeded, ProviderReference = "abc123" };
        mockPaymentRepository.Setup(r => r.GetLatestByOrderIdAsync(1)).ReturnsAsync(payment);

        var result = await service.GetLatestByOrderIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Succeeded, result!.Status);
    }

    [Fact]
    public async Task GetLatestByOrderIdAsync_NoPayment_ReturnsNull()
    {
        var (service, mockPaymentRepository, _, _) = CreateService();
        mockPaymentRepository.Setup(r => r.GetLatestByOrderIdAsync(1)).ReturnsAsync((Payment?)null);

        var result = await service.GetLatestByOrderIdAsync(1);

        Assert.Null(result);
    }
}
