using FlightKS.Enums;

namespace FlightKS.Models.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public required string BookingReference { get; set; } // e.g. "KS-ABC123"

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public BookingStatus Status { get; set; }
    public decimal TotalPrice { get; set; }
    public required string Currency { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BookingFlight> Flights { get; set; } = [];
    public ICollection<Passenger> Passengers { get; set; } = [];
    public Payment? Payment { get; set; }
}
