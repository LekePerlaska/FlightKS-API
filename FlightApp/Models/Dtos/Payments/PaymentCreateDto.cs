using FlightKS.Enums;

namespace FlightKS.Models.Dtos.Payments;

public class PaymentCreateDto
{
    public Guid BookingId { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public PaymentMethod Method { get; set; }
}
