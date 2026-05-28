using FlightKS.Models.Dtos.Admin;

namespace FlightKS.Services.Interfaces;

public interface IKeycloakService
{
    /// <summary>Creates a user in Keycloak and returns the new KeycloakUserId (UUID).</summary>
    Task<string> CreateUserAsync(
        string email,
        string fullName,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>Revokes the user's session in Keycloak using their refresh token.</summary>
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetUserRolesAsync(string keycloakUserId, CancellationToken cancellationToken = default);

    Task AssignUserRolesAsync(string keycloakUserId, IReadOnlyList<string> roleNames, CancellationToken cancellationToken = default);

    Task SetUserEnabledAsync(string keycloakUserId, bool enabled, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminRoleDto>> GetRealmRolesAsync(CancellationToken cancellationToken = default);
}
