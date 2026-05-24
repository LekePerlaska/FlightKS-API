using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FlightKS.Models.Config;
using FlightKS.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FlightKS.Services;

public class KeycloakService(
    HttpClient http,
    IOptions<KeycloakOptions> options,
    IMemoryCache cache) : IKeycloakService
{
    private readonly KeycloakOptions _opts = options.Value;

    public async Task<string> CreateUserAsync(
        string email,
        string fullName,
        string password,
        CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync(cancellationToken);

        var nameParts = fullName.Trim().Split(' ', 2);
        var payload = new
        {
            username = email,
            email,
            firstName = nameParts[0],
            lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
            enabled = true,
            emailVerified = true,
            requiredActions = Array.Empty<string>(),
            credentials = new[]
            {
                new { type = "password", value = password, temporary = false }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_opts.AdminApiBaseUrl}/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        request.Content = JsonContent.Create(payload);

        var response = await http.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            throw new InvalidOperationException("A user with this email already exists in Keycloak.");

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Keycloak user creation failed ({(int)response.StatusCode}): {body}");
        }

        var location = response.Headers.Location?.ToString()
            ?? throw new InvalidOperationException("Keycloak did not return a Location header.");

        return location.Split('/').Last();
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var form = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("client_id", _opts.FrontendClientId),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
        ]);

        // Best-effort: ignore Keycloak errors (token may already be expired)
        await http.PostAsync(_opts.LogoutUrl, form, cancellationToken);
    }

    private async Task<string> GetAdminTokenAsync(CancellationToken cancellationToken)
    {
        const string cacheKey = "keycloak_admin_token";

        if (cache.TryGetValue(cacheKey, out string? cachedToken))
            return cachedToken!;

        var form = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", _opts.AdminClientId),
            new KeyValuePair<string, string>("client_secret", _opts.AdminClientSecret),
        ]);

        var response = await http.PostAsync(_opts.TokenUrl, form, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var json = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Empty response from Keycloak token endpoint.");

        var token = json.RootElement.GetProperty("access_token").GetString()!;
        var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();

        cache.Set(cacheKey, token, TimeSpan.FromSeconds(expiresIn - 30));

        return token;
    }
}
