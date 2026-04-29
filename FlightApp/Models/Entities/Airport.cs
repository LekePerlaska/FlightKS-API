namespace FlightKS.Models.Entities;

public class Airport
{
    public Guid Id { get; set; }
    public required string IataCode { get; set; }    // e.g. "BEG", "TIA", "SKP"
    public required string Name { get; set; }         // e.g. "Nikola Tesla Airport"
    public required string City { get; set; }
    public required string Country { get; set; }
    public required string CountryCode { get; set; } // ISO 3166-1 alpha-2
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public required string TimeZone { get; set; }    // IANA tz, e.g. "Europe/Belgrade"

    public ICollection<Flight> DepartingFlights { get; set; } = [];
    public ICollection<Flight> ArrivingFlights { get; set; } = [];
}
