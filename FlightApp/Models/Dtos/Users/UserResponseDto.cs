namespace FlightKS.Models.Dtos.Users;

public class UserResponseDto
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public UserPassportDto? Passport { get; set; }
    public UserPreferencesDto? Preferences { get; set; }
    public UserLoyaltyDto? Loyalty { get; set; }
}
