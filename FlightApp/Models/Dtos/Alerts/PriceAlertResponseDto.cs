using FlightKS.Enums;
using FlightKS.Models.Dtos.Flights;

namespace FlightKS.Models.Dtos.Alerts;

public class PriceAlertResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required AirportDto Origin { get; set; }
    public required AirportDto Destination { get; set; }
    public CabinClass Cabin { get; set; }
    public decimal TargetPrice { get; set; }
    public decimal? CurrentPrice { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
}
