using System.Net;
using System.Net.Http.Json;
using FlightKS.IntegrationTests.Infrastructure;

namespace FlightKS.IntegrationTests;

/// <summary>
/// Verifies the integration test harness is correctly wired:
/// - Public endpoints return data without auth
/// - Protected endpoints require auth (401 without token)
/// - Protected endpoints succeed with the test auth scheme + seeded user
/// - Validation filter rejects malformed bodies with 400
/// </summary>
public class SmokeTests(IntegrationWebAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task PublicEndpoint_NoAuth_Returns200()
    {
        // GET /api/v1/airports is public (no RequireAuthorization)
        var response = await CreateAnonymousClient().GetAsync("/api/v1/airports");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProtectedEndpoint_NoAuth_Returns401()
    {
        // GET /api/v1/bookings/my requires RequireAuthorization(Policies.User)
        var response = await CreateAnonymousClient().GetAsync("/api/v1/bookings/my");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTestAuth_Returns200()
    {
        // TestUserId was seeded by IntegrationTestBase.InitializeAsync
        // TestAuthHandler sets sub = TestKeycloakId, roles = ["User"]
        // RequireCurrentUserFilter resolves the user from the DB → succeeds
        var response = await Client.GetAsync("/api/v1/bookings/my");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidationFilter_InvalidBody_Returns400()
    {
        // POST /api/v1/bookings with an empty itinerary ID should fail validation
        var payload = new { ItineraryId = Guid.Empty, PassengerCount = 0 };
        var response = await Client.PostAsJsonAsync("/api/v1/bookings", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("validation_error");
    }

    [Fact]
    public async Task AdminEndpoint_UserRole_Returns403()
    {
        // GET /api/v1/admin/users requires Admin role; default test user only has User
        var response = await Client.GetAsync("/api/v1/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminEndpoint_AdminRole_Returns200()
    {
        var client = CreateClientWithRoles("Admin");
        var response = await client.GetAsync("/api/v1/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheck_Returns200()
    {
        var response = await CreateAnonymousClient().GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
