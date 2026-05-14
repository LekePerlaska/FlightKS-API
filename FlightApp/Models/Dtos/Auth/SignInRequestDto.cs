namespace FlightKS.Models.Dtos.Auth;

public class SignInRequestDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}
