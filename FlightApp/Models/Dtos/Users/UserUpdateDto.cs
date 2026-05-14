namespace FlightKS.Models.Dtos.Users;

public class UserUpdateDto
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? PhoneNumber { get; set; }
}
