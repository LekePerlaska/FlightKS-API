using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace FlightKS.IntegrationTests.Infrastructure;

/// <summary>
/// Replaces Keycloak JWT Bearer in integration tests.
///
/// Only authenticates requests that carry an Authorization header
/// (value can be anything — the handler ignores the token content).
/// Requests without Authorization get NoResult → 401 from protected endpoints.
///
/// Roles come from the X-Test-Roles request header (comma-separated).
/// Absent → defaults to ["User"].
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "TestScheme";
    public const string TestKeycloakId = "test-keycloak-id-integration";
    public const string TestEmail = "integration@test.example";
    public const string TestFullName = "Integration Test User";

    /// <summary>Request header that carries comma-separated role names, e.g. "Admin,User".</summary>
    public const string RolesHeader = "X-Test-Roles";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Require an Authorization header; anonymous clients omit it → 401.
        if (!Request.Headers.ContainsKey("Authorization"))
            return Task.FromResult(AuthenticateResult.NoResult());

        var roles = Request.Headers.TryGetValue(RolesHeader, out var hv)
            ? hv.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : (string[])["User"];

        // realm_access JSON is consumed by KeycloakRoleClaimsTransformer → ClaimTypes.Role
        var realmAccess = JsonSerializer.Serialize(new { roles });

        var claims = new List<Claim>
        {
            new("sub", TestKeycloakId),
            new("email", TestEmail),
            new("name", TestFullName),
            new("preferred_username", TestEmail),
            new("realm_access", realmAccess),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>
    /// Returns an Authorization header value recognised by this handler.
    /// The token content is ignored — any non-empty value works.
    /// </summary>
    public static AuthenticationHeaderValue BearerHeader() =>
        new("Bearer", "test-token");
}
