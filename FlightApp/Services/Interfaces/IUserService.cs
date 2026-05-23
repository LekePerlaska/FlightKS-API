using FlightKS.Models.Entities;

namespace FlightKS.Services.Interfaces;

public interface IUserService
{
    Task<User> CreateAsync(
        string keycloakUserId,
        string fullName,
        string email,
        string? phoneNumber = null,
        DateOnly? dateOfBirth = null,
        string? passportNumber = null,
        string? nationality = null,
        CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<User?> GetByKeycloakIdAsync(string keycloakUserId, CancellationToken cancellationToken = default);

    Task<User?> UpdateAsync(
        Guid userId,
        string? fullName = null,
        string? phoneNumber = null,
        DateOnly? dateOfBirth = null,
        string? passportNumber = null,
        string? nationality = null,
        CancellationToken cancellationToken = default);
}
