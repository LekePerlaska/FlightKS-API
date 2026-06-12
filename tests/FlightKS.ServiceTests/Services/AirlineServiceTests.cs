using FlightKS.Exceptions;
using FlightKS.Services;
using FlightKS.ServiceTests.Fixtures;

namespace FlightKS.ServiceTests.Services;

public class AirlineServiceTests(PostgresFixture fixture) : ServiceTestBase(fixture)
{
    private static AirlineService MakeSut(FlightKS.Data.AppDbContext db) => new(db);

    [Fact]
    public async Task CreateAsync_DuplicateCode_ThrowsConflictException()
    {
        await using var setupDb = CreateContext();
        await new SeedData(setupDb).AirlineAsync("BA");

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.CreateAsync("BA", "Different Name", "UK", null))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsConflictException()
    {
        await using var setupDb = CreateContext();
        await new SeedData(setupDb).AirlineAsync("BA", "British Airways");

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.CreateAsync("EK", "British Airways", "UK", null))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateAsync_CodeUppercased()
    {
        await using var db = CreateContext();
        var airline = await MakeSut(db).CreateAsync("ba", "British Airways", "UK", null);
        airline.Code.Should().Be("BA");
    }

    [Fact]
    public async Task DeleteAsync_CascadesDeactivatingFlights()
    {
        await using var setupDb = CreateContext();
        var seed = new SeedData(setupDb);
        var airline = await seed.AirlineAsync();
        var origin = await seed.AirportAsync("ZZ1");
        var dest = await seed.AirportAsync("ZZ2");
        var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id);

        await using var db = CreateContext();
        await MakeSut(db).DeleteAsync(airline.Id);

        var savedFlight = await db.Flights.FindAsync(flight.Id);
        savedFlight!.IsActive.Should().BeFalse();
    }
}
