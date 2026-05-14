namespace FlightKS.Models.Entities;

public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public UserPassport? Passport { get; set; }
    public UserPreferences? Preferences { get; set; }
    public UserLoyalty? Loyalty { get; set; }

    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<SavedTraveler> SavedTravelers { get; set; } = [];
    public ICollection<PriceAlert> PriceAlerts { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<SavedFlight> SavedFlights { get; set; } = [];
    public ICollection<SearchHistory> SearchHistory { get; set; } = [];
}
