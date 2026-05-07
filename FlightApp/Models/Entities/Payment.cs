using FlightKS.Enums;

namespace FlightKS.Models.Entities;

public class Payment
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public decimal Amount { get; set; }
    public required string Currency { get; set; } // ISO 4217, e.g. "EUR"
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public string? TransactionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
