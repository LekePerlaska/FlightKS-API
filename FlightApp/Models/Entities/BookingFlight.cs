namespace FlightKS.Models.Entities;

public class BookingFlight
{
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public Guid FlightId { get; set; }
    public Flight Flight { get; set; } = null!;

    public int LegOrder { get; set; } // 1-based; outbound = 1, return = 2, etc.
}
