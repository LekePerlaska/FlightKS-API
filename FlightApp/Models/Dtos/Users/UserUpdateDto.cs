namespace FlightKS.Models.Dtos.Users;

public class UserUpdateDto
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
}
