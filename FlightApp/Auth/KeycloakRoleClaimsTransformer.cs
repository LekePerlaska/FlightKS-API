using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace FlightKS.Auth;

/// Keycloak stores realm roles under realm_access.roles as a JSON array.
/// ASP.NET Core needs role claims to use RequireRole / [Authorize(Roles = ...)].
public class KeycloakRoleClaimsTransformer : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return Task.FromResult(principal);

        var realmAccess = principal.FindFirst("realm_access")?.Value;
        if (string.IsNullOrEmpty(realmAccess)) return Task.FromResult(principal);

        using var doc = JsonDocument.Parse(realmAccess);
        if (!doc.RootElement.TryGetProperty("roles", out var roles) || roles.ValueKind != JsonValueKind.Array)
            return Task.FromResult(principal);

        foreach (var role in roles.EnumerateArray())
        {
            var value = role.GetString();
            if (!string.IsNullOrEmpty(value) && !identity.HasClaim(ClaimTypes.Role, value))
                identity.AddClaim(new Claim(ClaimTypes.Role, value));
        }

        return Task.FromResult(principal);
    }
}
