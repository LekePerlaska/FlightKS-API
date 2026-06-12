using FlightKS.Models.Dtos.Admin;
using FlightKS.Services.Interfaces;

namespace FlightKS.IntegrationTests.Infrastructure;

/// <summary>
/// No-op IKeycloakService for integration tests.
/// Endpoints that call Keycloak Admin APIs (e.g. GET /admin/users fetches roles per user)
/// use this stub so tests don't need a running Keycloak instance.
/// </summary>
internal sealed class StubKeycloakService : IKeycloakService
{
    public Task<string> CreateUserAsync(
        string email, string fullName, string password,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Guid.NewGuid().ToString());

    public Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<string>> GetUserRolesAsync(
        string keycloakUserId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>(["User"]);

    public Task AssignUserRolesAsync(
        string keycloakUserId, IReadOnlyList<string> roleNames,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task SetUserEnabledAsync(
        string keycloakUserId, bool enabled,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<AdminRoleDto>> GetRealmRolesAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AdminRoleDto>>([]);
}
