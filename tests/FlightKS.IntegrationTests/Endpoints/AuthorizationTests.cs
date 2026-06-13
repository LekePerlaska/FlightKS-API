using System.Net;
using FlightKS.IntegrationTests.Infrastructure;

namespace FlightKS.IntegrationTests.Endpoints;

/// <summary>
/// Verifies that role-based policies are correctly enforced across all three roles.
/// Uses header-based role injection via TestAuthHandler.
/// </summary>
public class AuthorizationTests(IntegrationWebAppFactory factory) : IntegrationTestBase(factory)
{
    // ── Admin policy ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("/api/v1/admin/airports")]
    [InlineData("/api/v1/admin/airlines")]
    [InlineData("/api/v1/admin/users")]
    [InlineData("/api/v1/admin/flights")]
    [InlineData("/api/v1/admin/flight-schedules")]
    public async Task AdminEndpoints_UserRole_Returns403(string url)
    {
        // Client has "User" role (default from IntegrationTestBase)
        var response = await Client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("/api/v1/admin/airports")]
    [InlineData("/api/v1/admin/airlines")]
    [InlineData("/api/v1/admin/flights")]
    [InlineData("/api/v1/admin/flight-schedules")]
    public async Task AdminEndpoints_AdminRole_Returns200(string url)
    {
        using var admin = CreateClientWithRoles("Admin");
        var response = await admin.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminUsers_AdminRole_Returns200()
    {
        using var admin = CreateClientWithRoles("Admin");
        var response = await admin.GetAsync("/api/v1/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── User policy ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UserEndpoint_NoAuth_Returns401()
    {
        var response = await CreateAnonymousClient().GetAsync("/api/v1/bookings/my");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UserEndpoint_UserRole_Returns200()
    {
        var response = await Client.GetAsync("/api/v1/bookings/my");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── FlightManager policy ─────────────────────────────────────────────────

    [Fact]
    public async Task FlightManagerEndpoints_UserRole_Returns403()
    {
        var response = await Client.GetAsync("/api/v1/flight-manager/flight-schedules");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FlightManagerEndpoints_FlightManagerRole_Returns200()
    {
        using var fm = CreateClientWithRoles("FlightManager");
        var response = await fm.GetAsync("/api/v1/flight-manager/flight-schedules");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Error response shape ─────────────────────────────────────────────────

    [Fact]
    public async Task ForbiddenResponse_HasCorrectErrorShape()
    {
        // 403s from the auth middleware go through UseStatusCodePages → application/json
        // (application/problem+json is only set by the real JwtBearer OnForbidden event)
        var response = await Client.GetAsync("/api/v1/admin/airports");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"status\":403");
        body.Should().Contain("\"code\":\"forbidden\"");
    }

    [Fact]
    public async Task UnauthorizedResponse_HasCorrectErrorShape()
    {
        var response = await CreateAnonymousClient().GetAsync("/api/v1/bookings/my");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"status\":401");
        body.Should().Contain("\"code\":\"unauthorized\"");
    }
}
