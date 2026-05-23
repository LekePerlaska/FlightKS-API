namespace FlightKS.Models.Entities;

public class Airline
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public required string Country { get; set; }
    public Guid? LogoFileId { get; set; }
    public UploadedFile? LogoFile { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<Aircraft> Aircrafts { get; set; } = [];
    public ICollection<Flight> Flights { get; set; } = [];
}
