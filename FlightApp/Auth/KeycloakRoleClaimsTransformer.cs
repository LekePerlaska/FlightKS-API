using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace FlightKS.Auth;

public class KeycloakRoleClaimsTransformer : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity)
            return Task.FromResult(principal);

        var realmAccessClaim = principal.FindFirstValue("realm_access");
        if (string.IsNullOrEmpty(realmAccessClaim))
            return Task.FromResult(principal);

        try
        {
            using var doc = JsonDocument.Parse(realmAccessClaim);
            if (doc.RootElement.TryGetProperty("roles", out var roles))
            {
                foreach (var role in roles.EnumerateArray())
                {
                    var roleName = role.GetString();
                    if (!string.IsNullOrEmpty(roleName))
                        identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                }
            }
        }
        catch (JsonException)
        {
            // Malformed claim — skip silently
        }

        return Task.FromResult(principal);
    }
}
