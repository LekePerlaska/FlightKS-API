namespace FlightKS.Models.Config;

public class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string Authority { get; init; } = string.Empty;
    public bool RequireHttpsMetadata { get; init; } = true;

    /// <summary>Base URL for Keycloak Admin REST API, e.g. http://localhost:8080/admin/realms/flightks</summary>
    public string AdminApiBaseUrl { get; init; } = string.Empty;

    /// <summary>Service-account client used by the backend to call Keycloak Admin APIs.</summary>
    public string AdminClientId { get; init; } = string.Empty;
    public string AdminClientSecret { get; init; } = string.Empty;

    /// <summary>Public (SPA) client used by the frontend — needed for the logout token revocation call.</summary>
    public string FrontendClientId { get; init; } = string.Empty;

    /// <summary>Audience value embedded in JWTs by the Keycloak audience mapper (e.g. "flightks-api").</summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Internal Keycloak base URL for backchannel calls (API → Keycloak token/logout endpoints).
    /// In Docker this is http://keycloak:8080/realms/{realm}; in local dev it equals Authority.
    /// When empty, falls back to Authority.
    /// </summary>
    public string InternalAuthority { get; init; } = string.Empty;

    private string BackchannelBase => string.IsNullOrEmpty(InternalAuthority) ? Authority : InternalAuthority;

    public string TokenUrl => $"{BackchannelBase}/protocol/openid-connect/token";
    public string LogoutUrl => $"{BackchannelBase}/protocol/openid-connect/logout";
}
