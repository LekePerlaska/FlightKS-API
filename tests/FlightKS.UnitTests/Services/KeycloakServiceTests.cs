using System.Net;
using System.Text;
using System.Text.Json;
using FlightKS.Exceptions;
using FlightKS.Models.Config;
using FlightKS.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FlightKS.UnitTests.Services;

public class KeycloakServiceTests
{
    private static readonly KeycloakOptions Opts = new()
    {
        Authority = "http://keycloak:8080/realms/test",
        AdminApiBaseUrl = "http://keycloak:8080/admin/realms/test",
        AdminClientId = "backend-client",
        AdminClientSecret = "secret",
        FrontendClientId = "frontend"
    };

    private static IMemoryCache NewCache() => new MemoryCache(new MemoryCacheOptions());

    // ── stub handler ────────────────────────────────────────────────────────

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(respond(request));
        }
    }

    private static string TokenJson(int expiresIn = 3600) =>
        JsonSerializer.Serialize(new { access_token = "tok-abc", expires_in = expiresIn });

    private static string RoleJson(string id = "role-id-1", string name = "User") =>
        JsonSerializer.Serialize(new { id, name });

    private static string RoleArrayJson() =>
        JsonSerializer.Serialize(new[] { new { id = "rid1", name = "User" } });

    private static HttpResponseMessage Json(string body, HttpStatusCode code = HttpStatusCode.OK) =>
        new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    // ── CreateUserAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsync_Success_ReturnsKeycloakUserId()
    {
        var userId = Guid.NewGuid().ToString();
        var handler = new StubHandler(req =>
        {
            // Token endpoint
            if (req.RequestUri!.AbsolutePath.Contains("openid-connect/token"))
                return Json(TokenJson());
            // User creation → 201 + Location header
            if (req.Method == HttpMethod.Post && req.RequestUri.AbsolutePath.Contains("/users") &&
                !req.RequestUri.AbsolutePath.Contains("role-mappings"))
            {
                var resp = new HttpResponseMessage(HttpStatusCode.Created);
                resp.Headers.Location = new Uri($"http://keycloak/users/{userId}");
                return resp;
            }
            // Role fetch
            if (req.Method == HttpMethod.Get && req.RequestUri.AbsolutePath.Contains("/roles/"))
                return Json(RoleJson());
            // Role assignment
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        var sut = new KeycloakService(
            new HttpClient(handler),
            Options.Create(Opts),
            NewCache());

        var result = await sut.CreateUserAsync("jane@example.com", "Jane Doe", "P@ssw0rd");

        result.Should().Be(userId);
    }

    [Fact]
    public async Task CreateUserAsync_Conflict_ThrowsConflictException()
    {
        var handler = new StubHandler(req =>
        {
            if (req.RequestUri!.AbsolutePath.Contains("openid-connect/token"))
                return Json(TokenJson());
            // Simulate Keycloak returning 409
            return new HttpResponseMessage(HttpStatusCode.Conflict);
        });

        var sut = new KeycloakService(new HttpClient(handler), Options.Create(Opts), NewCache());

        await sut.Invoking(s => s.CreateUserAsync("exists@example.com", "Existing", "P@ss"))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateUserAsync_ServerError_ThrowsInvalidOperationException()
    {
        var handler = new StubHandler(req =>
        {
            if (req.RequestUri!.AbsolutePath.Contains("openid-connect/token"))
                return Json(TokenJson());
            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Keycloak error", Encoding.UTF8, "text/plain")
            };
        });

        var sut = new KeycloakService(new HttpClient(handler), Options.Create(Opts), NewCache());

        await sut.Invoking(s => s.CreateUserAsync("fail@example.com", "Fail User", "P@ss"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*500*");
    }

    // ── Token caching ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetRealmRoles_CalledTwice_FetchesTokenOnce()
    {
        int tokenCalls = 0;
        var handler = new StubHandler(req =>
        {
            if (req.RequestUri!.AbsolutePath.Contains("openid-connect/token"))
            {
                tokenCalls++;
                return Json(TokenJson(expiresIn: 3600));
            }
            // Roles endpoint
            return Json(RoleArrayJson());
        });

        var cache = NewCache();
        var sut = new KeycloakService(new HttpClient(handler), Options.Create(Opts), cache);

        await sut.GetRealmRolesAsync();
        await sut.GetRealmRolesAsync();

        tokenCalls.Should().Be(1);
    }

    // ── LogoutAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_KeycloakReturnsError_DoesNotThrow()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var sut = new KeycloakService(new HttpClient(handler), Options.Create(Opts), NewCache());

        await sut.Invoking(s => s.LogoutAsync("some-refresh-token"))
            .Should().NotThrowAsync();
    }
}
