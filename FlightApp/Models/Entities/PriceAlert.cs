using FlightKS.Enums;

namespace FlightKS.Models.Entities;

public class PriceAlert
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public required string Origin { get; set; }        // FK → airports.code
    public Airport OriginAirport { get; set; } = null!;

    public required string Destination { get; set; }   // FK → airports.code
    public Airport DestinationAirport { get; set; } = null!;

    public CabinClass Cabin { get; set; }
    public decimal TargetPrice { get; set; }
    public decimal? CurrentPrice { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
