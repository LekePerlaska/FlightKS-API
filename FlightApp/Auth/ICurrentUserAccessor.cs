using FlightKS.Models.Entities;

namespace FlightKS.Auth;

public interface ICurrentUserAccessor
{
    string? KeycloakUserId { get; }
    bool IsAuthenticated { get; }
    Task<User?> GetUserAsync(CancellationToken cancellationToken = default);
    Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken = default);
}
