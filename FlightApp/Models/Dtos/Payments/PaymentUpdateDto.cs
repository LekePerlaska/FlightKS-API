using FlightKS.Enums;

namespace FlightKS.Models.Dtos.Payments;

public class PaymentUpdateDto
{
    public PaymentStatus Status { get; set; }
    public string? TransactionId { get; set; }
    public DateTime? CompletedAt { get; set; }
}
