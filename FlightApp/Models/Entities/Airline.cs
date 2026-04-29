namespace FlightKS.Models.Entities;

public class Airline
{
    public Guid Id { get; set; }
    public required string IataCode { get; set; }  // e.g. "JU" (Air Serbia), "LH" (Lufthansa)
    public required string Name { get; set; }
    public required string Country { get; set; }
    public string? LogoUrl { get; set; }

    public ICollection<Flight> Flights { get; set; } = [];
    public ICollection<BaggageOption> BaggageOptions { get; set; } = [];
}
