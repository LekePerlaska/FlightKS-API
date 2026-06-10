using System.Net;
using System.Net.Http.Json;
using FlightKS.IntegrationTests.Infrastructure;

namespace FlightKS.IntegrationTests.Endpoints;

public class PublicEndpointsTests(IntegrationWebAppFactory factory) : IntegrationTestBase(factory)
{
    // Use an anonymous client — these endpoints must work without auth
    private HttpClient Anon => CreateAnonymousClient();

    [Fact]
    public async Task GetAirports_NoAuth_Returns200WithArray()
    {
        await using var db = CreateDb();
        await new IntegrationSeeder(db).AirportAsync("LHR", "Heathrow", "London", "UK", "Europe/London");

        var response = await Anon.GetAsync("/api/v1/airports");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<List<object>>();
        json.Should().NotBeNull();
        json!.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetAirports_EmptyDb_Returns200EmptyArray()
    {
        var response = await Anon.GetAsync("/api/v1/airports");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("[]");
    }

    [Fact]
    public async Task AirportAutocomplete_Matches_Returns200()
    {
        await using var db = CreateDb();
        await new IntegrationSeeder(db).AirportAsync("LHR", "Heathrow", "London", "UK", "Europe/London");

        var response = await Anon.GetAsync("/api/v1/airports/autocomplete?q=Lon");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAirlines_NoAuth_Returns200()
    {
        var response = await Anon.GetAsync("/api/v1/airlines");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetFeaturedFlights_NoAuth_Returns200()
    {
        var response = await Anon.GetAsync("/api/v1/flights/featured");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPopularDestinations_NoAuth_Returns200()
    {
        var response = await Anon.GetAsync("/api/v1/flights/popular-destinations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FlightSearch_WithValidParams_Returns200()
    {
        await using var db = CreateDb();
        var seed = new IntegrationSeeder(db);
        var origin = await seed.AirportAsync("LHR");
        var dest = await seed.AirportAsync("DXB");

        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)).ToString("yyyy-MM-dd");
        var url = $"/api/v1/flights/search?originAirportId={origin.Id}&destinationAirportId={dest.Id}&date={date}";
        var response = await Anon.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResponseContentType_IsJson()
    {
        var response = await Anon.GetAsync("/api/v1/airports");
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }
}
