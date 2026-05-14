using FlightKS.Enums;

namespace FlightKS.Models.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public required string Reference { get; set; }     // e.g. "BKG-A1B2"

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public required string FlightId { get; set; }
    public Flight Flight { get; set; } = null!;

    public CabinClass Cabin { get; set; }
    public TripType TripType { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Upcoming;
    public DateOnly? ReturnDate { get; set; }
    public short PassengerCount { get; set; } = 1;

    public decimal BaseFare { get; set; }
    public decimal TaxesFees { get; set; }
    public decimal TotalPrice { get; set; }

    public DateTime BookedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BookingPassenger> Passengers { get; set; } = [];
}
