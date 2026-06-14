using System.Text.Json;
using FlightKS.Enums;
using FlightKS.IntegrationTests.Infrastructure;

namespace FlightKS.IntegrationTests.Endpoints.Admin;

public class AdminFlightSchedulesTests(IntegrationWebAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetAll_StatusLowercase_FiltersSchedules()
    {
        await using (var db = CreateDb())
        {
            var seed = new IntegrationSeeder(db);
            var airline = await seed.AirlineAsync("AS", "Admin Schedules");
            var aircraft = await seed.AircraftAsync(airline.Id);
            var origin = await seed.AirportAsync("ASA");
            var dest = await seed.AirportAsync("ASB");
            var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id, "AS100");
            await seed.ScheduleAsync(flight.Id, aircraft.Id);
            var delayed = await seed.ScheduleAsync(
                flight.Id,
                aircraft.Id,
                dep: new DateTime(2027, 7, 1, 8, 0, 0, DateTimeKind.Utc),
                arr: new DateTime(2027, 7, 1, 10, 0, 0, DateTimeKind.Utc));
            delayed.Status = FlightScheduleStatus.Delayed;
            await db.SaveChangesAsync();
        }

        using var admin = CreateClientWithRoles("Admin");
        var response = await admin.GetAsync("/api/v1/admin/flight-schedules?status=delayed");

        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("total").GetInt32().Should().Be(1);
        var item = body.RootElement.GetProperty("items")[0];
        item.GetProperty("status").GetString().Should().Be("delayed");
    }
}
