using Api.DTOs;
using Api.Mappers;
using Api.Models;
using Api.Models.Enums;
using Api.Repositories;

namespace Api.Services;

// Falsk/mock betalingssystem: der ringes ikke til nogen rigtig udbyder (Stripe, Adyen osv.),
// men flowet (opret forsøg -> "gennemfør" betaling -> opdatér ordre) matcher hvordan en rigtig
// integration ville se ud, så en rigtig gateway kan sættes ind senere uden at ændre resten af koden.
public class PaymentService(
    IPaymentRepository paymentRepository,
    IOrderRepository orderRepository,
    INotificationService notificationService) : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository = paymentRepository;
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly INotificationService _notificationService = notificationService;

    // Validerer ordren, kører den falske gateway og opdaterer ordrens status hvis betalingen lykkes.
    public async Task<PaymentDto> ProcessPaymentAsync(CreatePaymentDto dto)
    {
        var order = await _orderRepository.GetByIdAsync(dto.OrderId)
            ?? throw new KeyNotFoundException($"Order {dto.OrderId} not found.");

        if (order.Status != OrderStatus.AwaitingPayment)
            throw new InvalidOperationException("This order does not have a pending payment.");

        var (succeeded, providerReference) = SimulateCardCharge(dto.CardNumber);

        var payment = new Payment
        {
            OrderId = order.Id,
            // Beløbet hentes fra ordren (server-side), aldrig fra klienten.
            Amount = order.TotalPrice,
            Status = succeeded ? PaymentStatus.Succeeded : PaymentStatus.Failed,
            ProviderReference = providerReference,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _paymentRepository.CreateAsync(payment);

        if (succeeded)
        {
            await _orderRepository.UpdateStatusAsync(order.Id, OrderStatus.Pending);

            // Restauranten får først besked om ordren, når betalingen rent faktisk er gennemført.
            await _notificationService.NotifyNewOrderAsync(order.RestaurantId, order.Id);
        }

        return created.ToDto();
    }

    public async Task<PaymentDto?> GetLatestByOrderIdAsync(int orderId)
    {
        var payment = await _paymentRepository.GetLatestByOrderIdAsync(orderId);
        return payment?.ToDto();
    }

    // Den falske "betalingsgateway": intet rigtigt kort tjekkes nogen steder.
    // Regel til demo/test-brug: kortnumre der slutter på "0000" bliver afvist, alt andet godkendes.
    private static (bool Succeeded, string? ProviderReference) SimulateCardCharge(string cardNumber)
    {
        var succeeded = !cardNumber.EndsWith("0000");
        var providerReference = succeeded ? Guid.NewGuid().ToString("N") : null;
        return (succeeded, providerReference);
    }
}
