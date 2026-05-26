namespace FlightKS.Models.Entities;

public class Itinerary
{
    public Guid Id { get; set; }

    public Guid OriginAirportId { get; set; }
    public Airport OriginAirport { get; set; } = null!;

    public Guid DestinationAirportId { get; set; }
    public Airport DestinationAirport { get; set; } = null!;

    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public int TotalDurationMinutes { get; set; }
    public decimal TotalPrice { get; set; }
    public int StopsCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<ItinerarySegment> Segments { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
}
