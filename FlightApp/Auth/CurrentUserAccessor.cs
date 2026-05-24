using System.Security.Claims;
using FlightKS.Services.Interfaces;

namespace FlightKS.Auth;

public class CurrentUserAccessor(
    IHttpContextAccessor httpContextAccessor,
    IUserService userService) : ICurrentUserAccessor
{
    private ClaimsPrincipal User =>
        httpContextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException("No HTTP context available.");

    public string KeycloakUserId =>
        User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException("Token is missing the 'sub' claim.");

    public string Email =>
        User.FindFirstValue("email") ?? string.Empty;

    public string FullName =>
        User.FindFirstValue("name")
        ?? User.FindFirstValue("preferred_username")
        ?? string.Empty;

    public async Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken = default)
    {
        string keycloakId;
        try { keycloakId = KeycloakUserId; }
        catch { return null; }

        var user = await userService.GetByKeycloakIdAsync(keycloakId, cancellationToken);
        return user?.Id;
    }
}
