using FlightKS.Enums;

namespace FlightKS.Models.Entities;

public class SavedFlight
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public required string FlightId { get; set; }
    public Flight Flight { get; set; } = null!;

    public CabinClass Cabin { get; set; }
    public short Adults { get; set; } = 1;
    public DateOnly DepartDate { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public decimal PriceAtSave { get; set; }
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}
