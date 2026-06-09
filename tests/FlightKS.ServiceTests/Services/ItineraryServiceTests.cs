using FlightKS.Enums;
using FlightKS.Exceptions;
using FlightKS.Services;
using FlightKS.ServiceTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.ServiceTests.Services;

public class ItineraryServiceTests(PostgresFixture fixture) : ServiceTestBase(fixture)
{
    private static ItineraryService MakeSut(FlightKS.Data.AppDbContext db) => new(db);

    private async Task<(Guid AirportAId, Guid AirportBId, Guid AirportCId,
        Guid Schedule1Id, Guid Schedule2Id)> SeedTwoLegChainAsync(bool connected = true)
    {
        await using var db = CreateContext();
        var seed = new SeedData(db);

        var a = await seed.AirportAsync("IA1");
        var b = await seed.AirportAsync("IB2");
        var c = connected
            ? await seed.AirportAsync("IC3")
            : await seed.AirportAsync("IZ9");  // disconnected: B ≠ Z

        var airline = await seed.AirlineAsync("IT");
        var aircraft = await seed.AircraftAsync(airline.Id);

        // Flight1: A→B, Flight2: B→C (or A→Z if disconnected)
        var flight1 = await seed.FlightAsync(airline.Id, a.Id, b.Id, "IT001");
        var flight2 = connected
            ? await seed.FlightAsync(airline.Id, b.Id, c.Id, "IT002")
            : await seed.FlightAsync(airline.Id, a.Id, c.Id, "IT002"); // A→Z, doesn't connect B

        var dep1 = new DateTime(2026, 12, 1, 8, 0, 0, DateTimeKind.Utc);
        var arr1 = dep1.AddHours(3);
        var dep2 = arr1.AddHours(2);   // 2h layover
        var arr2 = dep2.AddHours(4);

        var s1 = await seed.ScheduleAsync(flight1.Id, aircraft.Id, dep1, arr1, currentPrice: 200m);
        var s2 = await seed.ScheduleAsync(flight2.Id, aircraft.Id, dep2, arr2, currentPrice: 300m);

        return (a.Id, b.Id, c.Id, s1.Id, s2.Id);
    }

    [Fact]
    public async Task CreateFromSchedulesAsync_ScheduleNotFound_ThrowsNotFoundException()
    {
        await using var db = CreateContext();
        await MakeSut(db).Invoking(s =>
                s.CreateFromSchedulesAsync([Guid.NewGuid()]))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateFromSchedulesAsync_SegmentsDontConnect_ThrowsValidationException()
    {
        var (_, _, _, s1Id, s2Id) = await SeedTwoLegChainAsync(connected: false);

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.CreateFromSchedulesAsync([s1Id, s2Id]))
            .Should().ThrowAsync<FlightKS.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task CreateFromSchedulesAsync_ConnectedChain_CreatesItineraryWithSegments()
    {
        var (aId, _, cId, s1Id, s2Id) = await SeedTwoLegChainAsync(connected: true);

        await using var db = CreateContext();
        var itin = await MakeSut(db).CreateFromSchedulesAsync([s1Id, s2Id]);

        itin.OriginAirportId.Should().Be(aId);
        itin.DestinationAirportId.Should().Be(cId);
        itin.StopsCount.Should().Be(1);
        itin.TotalPrice.Should().Be(500m); // 200 + 300
        itin.IsActive.Should().BeTrue();

        var segments = await db.ItinerarySegments
            .Where(seg => seg.ItineraryId == itin.Id)
            .OrderBy(seg => seg.SegmentOrder)
            .ToListAsync();
        segments.Should().HaveCount(2);
        segments[0].FlightScheduleId.Should().Be(s1Id);
        segments[1].FlightScheduleId.Should().Be(s2Id);
    }

    [Fact]
    public async Task CreateFromSchedulesAsync_SingleSchedule_CreatesDirectItinerary()
    {
        await using var setupDb = CreateContext();
        var seed = new SeedData(setupDb);
        var origin = await seed.AirportAsync("ID1");
        var dest = await seed.AirportAsync("ID2");
        var airline = await seed.AirlineAsync("ID");
        var aircraft = await seed.AircraftAsync(airline.Id);
        var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id);
        var dep = new DateTime(2027, 2, 1, 10, 0, 0, DateTimeKind.Utc);
        var schedule = await seed.ScheduleAsync(flight.Id, aircraft.Id, dep, dep.AddHours(2), currentPrice: 150m);

        await using var db = CreateContext();
        var itin = await MakeSut(db).CreateFromSchedulesAsync([schedule.Id]);

        itin.OriginAirportId.Should().Be(origin.Id);
        itin.DestinationAirportId.Should().Be(dest.Id);
        itin.TotalPrice.Should().Be(150m);
        itin.StopsCount.Should().Be(0);
    }
}
