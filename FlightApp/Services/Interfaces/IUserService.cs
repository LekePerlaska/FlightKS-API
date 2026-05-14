using FlightKS.Models.Dtos.Users;

namespace FlightKS.Services.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserResponseDto> CreateAsync(UserCreateDto dto, CancellationToken cancellationToken = default);
    Task<UserResponseDto?> UpdateAsync(Guid id, UserUpdateDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
