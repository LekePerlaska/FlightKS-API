using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FlightKS.IntegrationTests.Infrastructure;

namespace FlightKS.IntegrationTests.Endpoints;

public class BookingEndpointsTests(IntegrationWebAppFactory factory) : IntegrationTestBase(factory)
{
    // ── Seed helper ──────────────────────────────────────────────────────────

    private async Task<Guid> SeedItineraryAsync()
    {
        await using var db = CreateDb();
        var seed = new IntegrationSeeder(db);
        var origin = await seed.AirportAsync($"O{Guid.NewGuid().ToString()[..3].ToUpper()}");
        var dest   = await seed.AirportAsync($"D{Guid.NewGuid().ToString()[..3].ToUpper()}");
        var airline = await seed.AirlineAsync($"B{Guid.NewGuid().ToString()[..1].ToUpper()}");
        var aircraft = await seed.AircraftAsync(airline.Id);
        var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id);
        var schedule = await seed.ScheduleAsync(flight.Id, aircraft.Id);
        var (itin, _) = await seed.ItineraryAsync(origin.Id, dest.Id, schedule);
        return itin.Id;
    }

    // ── GET /bookings/my ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyBookings_NoBookings_Returns200EmptyArray()
    {
        var response = await Client.GetAsync("/api/v1/bookings/my");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("[]");
    }

    // ── POST /bookings ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBooking_EmptyItineraryId_Returns400()
    {
        var payload = new { ItineraryId = Guid.Empty, PassengerCount = 1 };
        var response = await Client.PostAsJsonAsync("/api/v1/bookings", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("validation_error");
    }

    [Fact]
    public async Task CreateBooking_PassengerCountZero_Returns400()
    {
        var payload = new { ItineraryId = Guid.NewGuid(), PassengerCount = 0 };
        var response = await Client.PostAsJsonAsync("/api/v1/bookings", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBooking_NonExistentItinerary_Returns404()
    {
        var payload = new { ItineraryId = Guid.NewGuid(), PassengerCount = 1 };
        var response = await Client.PostAsJsonAsync("/api/v1/bookings", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("not_found");
    }

    [Fact]
    public async Task CreateBooking_ValidItinerary_Returns201WithBookingReference()
    {
        var itinId = await SeedItineraryAsync();

        var payload = new { ItineraryId = itinId, PassengerCount = 2 };
        var response = await Client.PostAsJsonAsync("/api/v1/bookings", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("BKG-");
        body.Should().Contain("\"status\":\"pending\"");
    }

    [Fact]
    public async Task CreateBooking_ThenGetMy_ReturnsBookingInList()
    {
        var itinId = await SeedItineraryAsync();

        await Client.PostAsJsonAsync("/api/v1/bookings", new { ItineraryId = itinId, PassengerCount = 1 });

        var response = await Client.GetAsync("/api/v1/bookings/my");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("BKG-");
    }

    // ── Response shape ────────────────────────────────────────────────────────

    [Fact]
    public async Task NotFoundResponse_HasCorrectErrorShape()
    {
        var payload = new { ItineraryId = Guid.NewGuid(), PassengerCount = 1 };
        var response = await Client.PostAsJsonAsync("/api/v1/bookings", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(404);
        doc.RootElement.GetProperty("code").GetString().Should().Be("not_found");
        doc.RootElement.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ValidationErrorResponse_HasErrorsDictionary()
    {
        var payload = new { ItineraryId = Guid.Empty, PassengerCount = 0 };
        var response = await Client.PostAsJsonAsync("/api/v1/bookings", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("code").GetString().Should().Be("validation_error");
        doc.RootElement.GetProperty("errors").ValueKind.Should().Be(JsonValueKind.Object);
    }
}
