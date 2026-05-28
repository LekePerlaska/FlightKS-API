namespace FlightKS.Auth;

public interface ICurrentUserAccessor
{
    string KeycloakUserId { get; }
    string Email { get; }
    string FullName { get; }
    IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// Resolves the local DB user Id by looking up KeycloakUserId.
    /// Returns null if the user has no local DB record.
    /// </summary>
    Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken = default);
}
