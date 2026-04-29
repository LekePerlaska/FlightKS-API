namespace FlightKS.Models.Entities;

public class Flight
{
    public Guid Id { get; set; }
    public required string FlightNumber { get; set; } // e.g. "JU 450"

    public Guid AirlineId { get; set; }
    public Airline Airline { get; set; } = null!;

    public Guid AircraftId { get; set; }
    public Aircraft Aircraft { get; set; } = null!;

    public Guid DepartureAirportId { get; set; }
    public Airport DepartureAirport { get; set; } = null!;

    public Guid ArrivalAirportId { get; set; }
    public Airport ArrivalAirport { get; set; } = null!;

    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public FlightStatus Status { get; set; }

    public ICollection<FlightFare> Fares { get; set; } = [];
    public ICollection<BookingSegment> BookingSegments { get; set; } = [];
}
