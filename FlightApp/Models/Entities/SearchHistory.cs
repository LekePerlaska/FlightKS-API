using FlightKS.Enums;

namespace FlightKS.Models.Entities;

public class SearchHistory
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public required string Origin { get; set; }        // FK → airports.code
    public Airport OriginAirport { get; set; } = null!;

    public required string Destination { get; set; }   // FK → airports.code
    public Airport DestinationAirport { get; set; } = null!;

    public DateOnly DepartDate { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public TripType TripType { get; set; }
    public CabinClass Cabin { get; set; }
    public short Adults { get; set; } = 1;
    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
}
