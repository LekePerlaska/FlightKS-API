namespace FlightKS.Models.Entities;

public class Airport
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public required string City { get; set; }
    public required string Country { get; set; }
    public required string TimeZone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<Flight> OriginFlights { get; set; } = [];
    public ICollection<Flight> DestinationFlights { get; set; } = [];
}
