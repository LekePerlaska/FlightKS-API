using FlightKS.Enums;
using FlightKS.Models.Entities;

namespace FlightKS.Services.Interfaces;

public interface IPaymentService
{
    Task<Payment> CreateAsync(
        Guid bookingId,
        Guid ownerUserId,
        decimal amount,
        PaymentMethod method,
        string? transactionId = null,
        CancellationToken cancellationToken = default);

    Task<Payment?> GetByIdAsync(Guid paymentId, Guid? ownerUserId = null, CancellationToken cancellationToken = default);
    Task<PaymentRefund> CreateRefundAsync(Guid paymentId, decimal amount, string reason, CancellationToken cancellationToken = default);
}
