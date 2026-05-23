using FlightKS.Models.Dtos.Users;

namespace FlightKS.Models.Dtos.Auth;

public record AuthMeResponseDto(
    string KeycloakUserId,
    UserResponseDto? Profile);
