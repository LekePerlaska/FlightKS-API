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
}
