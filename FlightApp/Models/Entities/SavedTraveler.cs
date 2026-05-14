namespace FlightKS.Models.Entities;

public class SavedTraveler
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? PassportNumber { get; set; }
    public DateOnly? PassportExpiry { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
