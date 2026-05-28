using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FlightKS.Models.Config;
using FlightKS.Models.Dtos.Admin;
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

        var userId = location.Split('/').Last();

        await AssignRealmRoleAsync(userId, "User", adminToken, cancellationToken);

        return userId;
    }

    private async Task AssignRealmRoleAsync(
        string userId,
        string roleName,
        string adminToken,
        CancellationToken cancellationToken)
    {
        using var roleReq = new HttpRequestMessage(
            HttpMethod.Get, $"{_opts.AdminApiBaseUrl}/roles/{roleName}");
        roleReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var roleResp = await http.SendAsync(roleReq, cancellationToken);
        if (!roleResp.IsSuccessStatusCode) return;

        using var roleDoc = await roleResp.Content.ReadFromJsonAsync<JsonDocument>(
            cancellationToken: cancellationToken);
        if (roleDoc is null) return;

        var roleId = roleDoc.RootElement.GetProperty("id").GetString();

        using var assignReq = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_opts.AdminApiBaseUrl}/users/{userId}/role-mappings/realm");
        assignReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        assignReq.Content = JsonContent.Create(new[]
        {
            new { id = roleId, name = roleName }
        });

        await http.SendAsync(assignReq, cancellationToken);
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

    public async Task<IReadOnlyList<string>> GetUserRolesAsync(
        string keycloakUserId,
        CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync(cancellationToken);

        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_opts.AdminApiBaseUrl}/users/{keycloakUserId}/role-mappings/realm");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var resp = await http.SendAsync(req, cancellationToken);
        if (!resp.IsSuccessStatusCode) return [];

        using var doc = await resp.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
        if (doc is null) return [];

        return doc.RootElement.EnumerateArray()
            .Select(e => e.GetProperty("name").GetString() ?? string.Empty)
            .Where(n => n.Length > 0)
            .ToList();
    }

    public async Task AssignUserRolesAsync(
        string keycloakUserId,
        IReadOnlyList<string> roleNames,
        CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync(cancellationToken);

        // Fetch all realm roles to get their IDs
        var allRoles = await GetRealmRolesInternalAsync(adminToken, cancellationToken);
        var roleMap = allRoles.ToDictionary(r => r.Name, r => r.Id);

        // Get current roles so we can compute what to remove
        var current = await GetUserRolesAsync(keycloakUserId, cancellationToken);

        var toAdd = roleNames.Except(current).ToList();
        var toRemove = current.Except(roleNames).ToList();

        if (toRemove.Count > 0)
        {
            var removePayload = toRemove
                .Where(roleMap.ContainsKey)
                .Select(n => new { id = roleMap[n], name = n })
                .ToArray();

            using var delReq = new HttpRequestMessage(
                HttpMethod.Delete,
                $"{_opts.AdminApiBaseUrl}/users/{keycloakUserId}/role-mappings/realm");
            delReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            delReq.Content = JsonContent.Create(removePayload);
            await http.SendAsync(delReq, cancellationToken);
        }

        if (toAdd.Count > 0)
        {
            var addPayload = toAdd
                .Where(roleMap.ContainsKey)
                .Select(n => new { id = roleMap[n], name = n })
                .ToArray();

            using var addReq = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_opts.AdminApiBaseUrl}/users/{keycloakUserId}/role-mappings/realm");
            addReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            addReq.Content = JsonContent.Create(addPayload);
            await http.SendAsync(addReq, cancellationToken);
        }
    }

    public async Task SetUserEnabledAsync(
        string keycloakUserId,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync(cancellationToken);

        using var req = new HttpRequestMessage(
            HttpMethod.Put,
            $"{_opts.AdminApiBaseUrl}/users/{keycloakUserId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        req.Content = JsonContent.Create(new { enabled });

        await http.SendAsync(req, cancellationToken);
    }

    public async Task<IReadOnlyList<AdminRoleDto>> GetRealmRolesAsync(CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync(cancellationToken);
        return await GetRealmRolesInternalAsync(adminToken, cancellationToken);
    }

    private async Task<IReadOnlyList<AdminRoleDto>> GetRealmRolesInternalAsync(
        string adminToken,
        CancellationToken cancellationToken)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{_opts.AdminApiBaseUrl}/roles");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var resp = await http.SendAsync(req, cancellationToken);
        if (!resp.IsSuccessStatusCode) return [];

        using var doc = await resp.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
        if (doc is null) return [];

        return doc.RootElement.EnumerateArray()
            .Select(e => new AdminRoleDto(
                e.GetProperty("id").GetString() ?? string.Empty,
                e.GetProperty("name").GetString() ?? string.Empty))
            .Where(r => r.Id.Length > 0 && r.Name.Length > 0)
            .ToList();
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
