namespace FlightKS.Models.Entities;

public class BookingSegment
{
    public Guid Id { get; set; }
    public int SegmentOrder { get; set; } // 1-based; defines the leg sequence within a booking

    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public Guid FlightId { get; set; }
    public Flight Flight { get; set; } = null!;

    public Guid FlightFareId { get; set; }
    public FlightFare FlightFare { get; set; } = null!;

    public ICollection<PassengerSegment> PassengerSegments { get; set; } = [];
}
