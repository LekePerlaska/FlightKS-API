namespace FlightKS.Models.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public required string BookingReference { get; set; } // e.g. "KS-ABC123"

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public TripType TripType { get; set; }
    public BookingStatus Status { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Ordered list of flight legs (1 for one-way, 2 for round-trip, N for multi-stop)
    public ICollection<BookingSegment> Segments { get; set; } = [];
    public ICollection<Passenger> Passengers { get; set; } = [];
    public Payment? Payment { get; set; }
}
