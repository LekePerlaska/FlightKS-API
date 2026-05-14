using FlightKS.Enums;

namespace FlightKS.Models.Dtos.Flights;

public class FlightUpdateDto
{
    public required string FlightNumber { get; set; }

    public required string AirlineName { get; set; }
    public required string AirlineCode { get; set; }
    public string? AirlineLogoUrl { get; set; }

    public required string DepartureAirportCode { get; set; }
    public required string DepartureCity { get; set; }
    public DateTime DepartureTime { get; set; }

    public required string ArrivalAirportCode { get; set; }
    public required string ArrivalCity { get; set; }
    public DateTime ArrivalTime { get; set; }

    public CabinClass CabinClass { get; set; }
    public decimal Price { get; set; }
    public required string Currency { get; set; }

    public decimal CabinBaggageKg { get; set; }
    public decimal CheckedBaggageKg { get; set; }

    public FlightStatus Status { get; set; }
}
