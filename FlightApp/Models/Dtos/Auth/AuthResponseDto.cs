using FlightKS.Models.Dtos.Users;

namespace FlightKS.Models.Dtos.Auth;

public class AuthResponseDto
{
    public required UserResponseDto User { get; set; }
    public required string Token { get; set; }
}
