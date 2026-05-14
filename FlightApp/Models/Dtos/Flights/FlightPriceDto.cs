using FlightKS.Enums;

namespace FlightKS.Models.Dtos.Flights;

public class FlightPriceDto
{
    public CabinClass Cabin { get; set; }
    public decimal Price { get; set; }
    public short TotalSeats { get; set; }
}
