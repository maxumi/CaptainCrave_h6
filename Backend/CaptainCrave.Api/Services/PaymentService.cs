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

    /// <summary>
    /// Tjekker at ordren findes og rent faktisk afventer betaling, kører den falske gateway,
    /// og hvis betalingen lykkes: skifter ordren til "Pending" og gør restauranten opmærksom
    /// på den nye ordre.
    /// </summary>
    /// <returns>Betalingsforsøget som DTO (med status Succeeded eller Failed).</returns>
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

    /// <summary>
    /// Henter det seneste betalingsforsøg for en ordre, så brugeren kan se om betalingen
    /// lykkedes, fejlede, eller stadig venter.
    /// </summary>
    /// <returns>Det seneste betalingsforsøg som DTO, eller null hvis der ikke er forsøgt betalt endnu.</returns>
    public async Task<PaymentDto?> GetLatestByOrderIdAsync(int orderId)
    {
        var payment = await _paymentRepository.GetLatestByOrderIdAsync(orderId);
        return payment?.ToDto();
    }

    /// <summary>
    /// Den falske "betalingsgateway": der bliver IKKE tjekket noget rigtigt kort nogen steder.
    /// Simpel regel til demo/test-brug: kortnumre der slutter på "0000" bliver afvist,
    /// alt andet bliver godkendt.
    /// </summary>
    /// <returns>Om betalingen lykkedes, og en falsk kvitteringskode hvis den gjorde.</returns>
    private static (bool Succeeded, string? ProviderReference) SimulateCardCharge(string cardNumber)
    {
        var succeeded = !cardNumber.EndsWith("0000");
        var providerReference = succeeded ? Guid.NewGuid().ToString("N") : null;
        return (succeeded, providerReference);
    }
}
