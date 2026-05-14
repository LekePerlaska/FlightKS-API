namespace FlightKS.Models.Entities;

public class Flight
{
    public required string Id { get; set; }            // opaque API id, PK

    public required string Origin { get; set; }        // FK → airports.code
    public Airport OriginAirport { get; set; } = null!;

    public required string Destination { get; set; }   // FK → airports.code
    public Airport DestinationAirport { get; set; } = null!;

    public required string Airline { get; set; }       // e.g. "Air Albania"
    public required string FlightNumber { get; set; }  // e.g. "AK 123"

    public string Currency { get; set; } = "USD";
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public int DurationMinutes { get; set; }
    public short Stops { get; set; }
    public bool BaggageIncluded { get; set; }
    public bool Refundable { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FlightPrice> Prices { get; set; } = [];
}
