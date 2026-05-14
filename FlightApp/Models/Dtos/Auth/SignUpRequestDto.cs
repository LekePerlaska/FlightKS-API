namespace FlightKS.Models.Dtos.Auth;

public class SignUpRequestDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }  // ≥ 8 chars
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
}
