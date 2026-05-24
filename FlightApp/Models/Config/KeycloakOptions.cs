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

    public string TokenUrl => $"{Authority}/protocol/openid-connect/token";
    public string LogoutUrl => $"{Authority}/protocol/openid-connect/logout";
}
