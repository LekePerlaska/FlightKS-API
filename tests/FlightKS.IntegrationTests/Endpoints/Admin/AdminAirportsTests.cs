using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FlightKS.IntegrationTests.Infrastructure;

namespace FlightKS.IntegrationTests.Endpoints.Admin;

public class AdminAirportsTests(IntegrationWebAppFactory factory) : IntegrationTestBase(factory)
{
    private HttpClient AdminClient => CreateClientWithRoles("Admin");

    // ── GET ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Admin_Returns200WithArray()
    {
        await using var db = CreateDb();
        await new IntegrationSeeder(db).AirportAsync("TST", "Test Airport", "Testville", "TC", "Europe/London");

        var response = await AdminClient.GetAsync("/api/v1/admin/airports");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("TST");
    }

    // ── POST (create) ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_Valid_Returns201WithLocationHeader()
    {
        var payload = new { Code = "CDG", Name = "Charles de Gaulle", City = "Paris", Country = "France", TimeZone = "Europe/Paris" };

        var response = await AdminClient.PostAsJsonAsync("/api/v1/admin/airports", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/v1/admin/airports/");
    }

    [Fact]
    public async Task Create_Valid_ResponseBodyMatchesInput()
    {
        var payload = new { Code = "JFK", Name = "JFK Airport", City = "New York", Country = "USA", TimeZone = "America/New_York" };

        var response = await AdminClient.PostAsJsonAsync("/api/v1/admin/airports", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"JFK\"");
        body.Should().Contain("New York");
    }

    [Fact]
    public async Task Create_InvalidBody_Returns400WithValidationErrors()
    {
        var payload = new { Code = "", Name = "", City = "Paris", Country = "France", TimeZone = "Europe/Paris" };

        var response = await AdminClient.PostAsJsonAsync("/api/v1/admin/airports", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"code\":\"validation_error\"");
        body.Should().Contain("\"errors\"");
    }

    [Fact]
    public async Task Create_InvalidTimezone_Returns400WithTimezoneError()
    {
        var payload = new { Code = "ZZZ", Name = "Test", City = "City", Country = "Country", TimeZone = "Not/AZone" };

        var response = await AdminClient.PostAsJsonAsync("/api/v1/admin/airports", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("IANA");
    }

    [Fact]
    public async Task Create_DuplicateCode_Returns409WithConflictCode()
    {
        await using var db = CreateDb();
        await new IntegrationSeeder(db).AirportAsync("DUP");

        var payload = new { Code = "DUP", Name = "Duplicate", City = "City", Country = "Country", TimeZone = "Europe/London" };
        var response = await AdminClient.PostAsJsonAsync("/api/v1/admin/airports", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"code\":\"conflict\"");
    }

    // ── PUT (update) ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ExistingAirport_Returns200WithNewName()
    {
        await using var db = CreateDb();
        var airport = await new IntegrationSeeder(db).AirportAsync("UPD", "Old Name", "City", "UK", "Europe/London");

        var payload = new { Name = "New Name" };
        var response = await AdminClient.PutAsJsonAsync($"/api/v1/admin/airports/{airport.Id}", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("New Name");
    }

    [Fact]
    public async Task Update_NonExistentId_Returns404()
    {
        var payload = new { Name = "Whatever" };
        var response = await AdminClient.PutAsJsonAsync($"/api/v1/admin/airports/{Guid.NewGuid()}", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingUnusedAirport_Returns204()
    {
        await using var db = CreateDb();
        var airport = await new IntegrationSeeder(db).AirportAsync("DEL");

        var response = await AdminClient.DeleteAsync($"/api/v1/admin/airports/{airport.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_NonExistentId_Returns404()
    {
        var response = await AdminClient.DeleteAsync($"/api/v1/admin/airports/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
