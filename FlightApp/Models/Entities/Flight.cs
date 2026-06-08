namespace FlightKS.Models.Entities;

public class Flight
{
    public Guid Id { get; set; }

    public Guid AirlineId { get; set; }
    public Airline Airline { get; set; } = null!;

    public required string FlightNumber { get; set; }

    public Guid OriginAirportId { get; set; }
    public Airport OriginAirport { get; set; } = null!;

    public Guid DestinationAirportId { get; set; }
    public Airport DestinationAirport { get; set; } = null!;

    public decimal BasePrice { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<FlightSchedule> FlightSchedules { get; set; } = [];
}
