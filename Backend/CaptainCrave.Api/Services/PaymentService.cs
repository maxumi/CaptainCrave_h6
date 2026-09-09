using Api.DTOs;
using Api.Mappers;
using Api.Models;
using Api.Models.Enums;
using Api.Repositories;

namespace Api.Services;

// Falsk/mock betalingssystem, fordi vi ikke vil have en rigtig betalingsgateway (Stripe, paypal osv.).
// Flowet (opret forsøg -> "gennemfør" betaling -> opdater ordre) matcher hvordan en rigtig

// primary constructor, som tager tre dependencies ind.
public class PaymentService(
    IPaymentRepository paymentRepository,
    IOrderRepository orderRepository,
    INotificationService notificationService) : IPaymentService
{
    // DI: gemmer dependencies i private fields (encapsu)
    private readonly IPaymentRepository _paymentRepository = paymentRepository;
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly INotificationService _notificationService = notificationService;

    // Gennemfører et betalingsforsøg for en ordre.
    public async Task<PaymentDto> ProcessPaymentAsync(CreatePaymentDto dto)
    {
        // Finder ordren i databasen ud fra det angivne OrderId. 
        var order = await _orderRepository.GetByIdAsync(dto.OrderId)
            ?? throw new KeyNotFoundException($"Order {dto.OrderId} not found.");

        // checker om ordren afventer betaling. Hvis ikke, kastes en InvalidOperationException.
        if (order.Status != OrderStatus.AwaitingPayment)
            throw new InvalidOperationException("This order does not have a pending payment.");

        // Kører den simulerede kortbetaling.
        // (tuple destructuring) destrukturerer resultatet.
        var (succeeded, providerReference) = SimulateCardCharge(dto.CardNumber);

        // laver ny payment object, som repræsenterer betalingsforsøget.
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
            // opdatere ordren til pending status, da betalingen er lykkedes.
            await _orderRepository.UpdateStatusAsync(order.Id, OrderStatus.Pending);

            // restaurant notificeres om den nye ordre.
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
        return (succeeded, providerReference); // returnerer resultat som tuple
    }
}
