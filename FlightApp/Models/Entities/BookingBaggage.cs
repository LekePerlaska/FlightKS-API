namespace FlightKS.Models.Entities;

public class BookingBaggage
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public Guid PassengerId { get; set; }
    public Passenger Passenger { get; set; } = null!;

    public Guid BaggageOptionId { get; set; }
    public BaggageOption BaggageOption { get; set; } = null!;

    public int Quantity { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
