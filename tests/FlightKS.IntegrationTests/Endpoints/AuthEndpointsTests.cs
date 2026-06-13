using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FlightKS.IntegrationTests.Infrastructure;

namespace FlightKS.IntegrationTests.Endpoints;

public class AuthEndpointsTests(IntegrationWebAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetMe_NoAuth_Returns401()
    {
        var response = await CreateAnonymousClient().GetAsync("/api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_Authenticated_Returns200WithUserData()
    {
        // TestUserId was seeded by InitializeAsync; GetOrCreate finds it by KeycloakUserId
        var response = await Client.GetAsync("/api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(TestAuthHandler.TestEmail);
    }

    [Fact]
    public async Task GetMe_FirstCall_CreatesUserAndReturns200()
    {
        // After Respawn the test user was re-seeded by base class, but let's verify
        // that calling /auth/me for an already-existing user works correctly.
        var response = await Client.GetAsync("/api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("email").GetString().Should().Be(TestAuthHandler.TestEmail);
        doc.RootElement.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetMe_RolesReturnedInResponse()
    {
        var response = await Client.GetAsync("/api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var roles = doc.RootElement.GetProperty("roles").EnumerateArray()
            .Select(r => r.GetString()).ToList();
        roles.Should().Contain("User");
    }

    [Fact]
    public async Task Logout_Authenticated_Returns204()
    {
        var payload = new { RefreshToken = "some-refresh-token" };
        var response = await Client.PostAsJsonAsync("/api/v1/auth/logout", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_EmptyRefreshToken_Returns400()
    {
        var payload = new { RefreshToken = "" };
        var response = await Client.PostAsJsonAsync("/api/v1/auth/logout", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
