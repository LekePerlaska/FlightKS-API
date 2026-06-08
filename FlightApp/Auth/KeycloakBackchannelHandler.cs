namespace FlightKS.Auth;

/// <summary>
/// Rewrites localhost:8080 → keycloak:8080 for OIDC backchannel calls (metadata, JWKS).
/// Needed when the API runs in Docker: Keycloak's discovery document returns localhost URLs
/// for jwks_uri, but localhost:8080 inside the container doesn't resolve to Keycloak.
/// </summary>
public class KeycloakBackchannelHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is { Host: "localhost", Port: 8080 })
        {
            request.RequestUri = new UriBuilder(request.RequestUri) { Host = "keycloak" }.Uri;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
