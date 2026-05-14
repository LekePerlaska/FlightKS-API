using FlightKS.Models.Dtos.Auth;

namespace FlightKS.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> SignUpAsync(SignUpRequestDto dto, CancellationToken cancellationToken = default);
    Task<AuthResponseDto?> SignInAsync(SignInRequestDto dto, CancellationToken cancellationToken = default);
}
