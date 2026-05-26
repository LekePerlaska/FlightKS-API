using FlightKS.Enums;

namespace FlightKS.Models.Entities;

public class Booking
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid? ItineraryId { get; set; }
    public Itinerary? Itinerary { get; set; }

    public required string BookingReference { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<Passenger> Passengers { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
    public ICollection<BookingBaggage> BookingBaggage { get; set; } = [];
}
