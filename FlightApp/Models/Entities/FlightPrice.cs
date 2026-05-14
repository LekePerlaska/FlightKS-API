using FlightKS.Enums;

namespace FlightKS.Models.Entities;

public class FlightPrice
{
    public required string FlightId { get; set; }
    public Flight Flight { get; set; } = null!;

    public CabinClass Cabin { get; set; }
    public decimal Price { get; set; }
    public short TotalSeats { get; set; }
}
