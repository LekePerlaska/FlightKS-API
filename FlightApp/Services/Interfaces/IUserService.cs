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

    Task<User> GetOrCreateAsync(
        string keycloakUserId,
        string email,
        string fullName,
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

    Task<(IReadOnlyList<User> Items, int Total)> GetAllForAdminAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<User?> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);
    Task<User?> SetAirlineAsync(Guid userId, Guid? airlineId, CancellationToken cancellationToken = default);
}
