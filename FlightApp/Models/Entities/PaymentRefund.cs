using FlightKS.Enums;

namespace FlightKS.Models.Entities;

public class PaymentRefund
{
    public Guid Id { get; set; }

    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;

    public decimal Amount { get; set; }
    public required string Reason { get; set; }
    public RefundStatus RefundStatus { get; set; } = RefundStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
