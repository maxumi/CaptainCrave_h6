using Api.DTOs;
using Api.Mappers;
using Api.Models;
using Api.Models.Enums;
using Api.Repositories;

namespace Api.Services;

// Falsk/mock betalingssystem: der ringes ikke til nogen rigtig udbyder (Stripe, Adyen osv.).
// Flowet (opret forsøg -> "gennemfør" betaling -> opdatér ordre) matcher hvordan en rigtig
// integration ville se ud, så en rigtig gateway kan sættes ind senere uden at ændre resten af koden.

// primary constructor, som tager tre afhængigheder ind.
public class PaymentService(
    IPaymentRepository paymentRepository,
    IOrderRepository orderRepository,
    INotificationService notificationService) : IPaymentService
{
    // DI: gemmer afhængighederne, så metoderne i klassen kan bruge dem.
    private readonly IPaymentRepository _paymentRepository = paymentRepository;
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly INotificationService _notificationService = notificationService;

    // Gennemfører et betalingsforsøg for en ordre.
    public async Task<PaymentDto> ProcessPaymentAsync(CreatePaymentDto dto)
    {
        // Finder ordren i databasen ud fra det angivne OrderId. 
        // Hvis ordren ikke findes, kastes en KeyNotFoundException.
        var order = await _orderRepository.GetByIdAsync(dto.OrderId)
            ?? throw new KeyNotFoundException($"Order {dto.OrderId} not found.");

        // Kun ordrer der afventer betaling, må betales.
        // Hvis ordren ikke afventer betaling, kastes en InvalidOperationException.
        if (order.Status != OrderStatus.AwaitingPayment)
            throw new InvalidOperationException("This order does not have a pending payment.");

        // Kører den falske betalingsgateway.
        var (succeeded, providerReference) = SimulateCardCharge(dto.CardNumber);

        // ny payment 
        var payment = new Payment
        {
            OrderId = order.Id,
            Amount = order.TotalPrice, // Beløbet hentes fra ordren, aldrig fra klienten.
            Status = succeeded ? PaymentStatus.Succeeded : PaymentStatus.Failed,
            ProviderReference = providerReference,
            CreatedAt = DateTime.UtcNow
        };

        // Gemmer forsøget i databasen, uanset om det lykkedes eller ej.
        var created = await _paymentRepository.CreateAsync(payment);
        
        // Hvis betalingen lykkedes, opdateres ordren og restauranten notificeres.
        if (succeeded)
        {
            // Kun en succes flytter ordren videre. Et fejlet forsøg gemmes som historik.
            await _orderRepository.UpdateStatusAsync(order.Id, OrderStatus.Pending);

            // Restauranten får først besked om ordren, når betalingen rent faktisk er gennemført.
            await _notificationService.NotifyNewOrderAsync(order.RestaurantId, order.Id);
        }


        // Returnerer det oprettede betalingsforsøg som DTO, uanset om det lykkedes eller ej.
        return created.ToDto();
    }

    // Henter det seneste betalingsforsøg, så brugeren kan se om det lykkedes, fejlede eller venter.
    public async Task<PaymentDto?> GetLatestByOrderIdAsync(int orderId)
    {
        var payment = await _paymentRepository.GetLatestByOrderIdAsync(orderId);
        return payment?.ToDto();
    }

    // Falsk gateway: kort der slutter på "0000" fejler, alt andet bliver godkendt.
    private static (bool Succeeded, string? ProviderReference) SimulateCardCharge(string cardNumber)
    {
        var succeeded = !cardNumber.EndsWith("0000");
        var providerReference = succeeded ? Guid.NewGuid().ToString("N") : null;
        return (succeeded, providerReference);
    }
}
