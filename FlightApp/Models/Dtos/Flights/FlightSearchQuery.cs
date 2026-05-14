using FlightKS.Enums;

namespace FlightKS.Models.Dtos.Flights;

public class FlightSearchQuery
{
    public required string Origin { get; set; }
    public required string Destination { get; set; }
    public DateOnly Date { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public CabinClass Cabin { get; set; } = CabinClass.Economy;
    public short Adults { get; set; } = 1;
    public TripType TripType { get; set; } = TripType.OneWay;
}
