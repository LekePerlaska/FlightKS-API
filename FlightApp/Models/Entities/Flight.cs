using FlightKS.Enums;

namespace FlightKS.Models.Entities;

public class Flight
{
    public Guid Id { get; set; }
    public required string FlightNumber { get; set; }      // e.g. "JU 450"

    public required string AirlineName { get; set; }
    public required string AirlineCode { get; set; }       // IATA, e.g. "JU"
    public string? AirlineLogoUrl { get; set; }

    public required string DepartureAirportCode { get; set; } // e.g. "BEG"
    public required string DepartureCity { get; set; }
    public DateTime DepartureTime { get; set; }

    public required string ArrivalAirportCode { get; set; }   // e.g. "FRA"
    public required string ArrivalCity { get; set; }
    public DateTime ArrivalTime { get; set; }

    public CabinClass CabinClass { get; set; }
    public decimal Price { get; set; }
    public required string Currency { get; set; }          // ISO 4217, e.g. "EUR"

    public decimal CabinBaggageKg { get; set; }
    public decimal CheckedBaggageKg { get; set; }

    public FlightStatus Status { get; set; }

    public ICollection<BookingFlight> BookingFlights { get; set; } = [];
}
