using FlightKS.Enums;

namespace FlightKS.Models.Dtos.Alerts;

public class PriceAlertCreateDto
{
    public required string Origin { get; set; }
    public required string Destination { get; set; }
    public CabinClass Cabin { get; set; }
    public decimal TargetPrice { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}
