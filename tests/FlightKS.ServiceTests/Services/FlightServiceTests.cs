using FlightKS.Exceptions;
using FlightKS.Services;
using FlightKS.ServiceTests.Fixtures;

namespace FlightKS.ServiceTests.Services;

public class FlightServiceTests(PostgresFixture fixture) : ServiceTestBase(fixture)
{
    private static FlightService MakeSut(FlightKS.Data.AppDbContext db) => new(db);

    [Fact]
    // The service-level same-origin/dest guard exists in UpdateAsync; CreateAsync relies on the validator.
    // This test verifies UpdateAsync rejects the change when both airport IDs are identical.
    public async Task UpdateAsync_SameOriginDestination_ThrowsValidationException()
    {
        await using var setupDb = CreateContext();
        var seed = new SeedData(setupDb);
        var airline = await seed.AirlineAsync();
        var origin = await seed.AirportAsync("O5A");
        var dest = await seed.AirportAsync("D5B");
        var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id);

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s =>
                s.UpdateAsync(flight.Id, null, null, origin.Id, origin.Id, null, null))
            .Should().ThrowAsync<FlightKS.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_DuplicateFlightNumberForAirline_ThrowsConflictException()
    {
        await using var setupDb = CreateContext();
        var seed = new SeedData(setupDb);
        var airline = await seed.AirlineAsync();
        var origin = await seed.AirportAsync("O1A");
        var dest = await seed.AirportAsync("D1B");
        await seed.FlightAsync(airline.Id, origin.Id, dest.Id, "BA999");

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.CreateAsync(airline.Id, "BA999", origin.Id, dest.Id, 100m))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateAsync_AirlineNotFound_ThrowsNotFoundException()
    {
        await using var setupDb = CreateContext();
        var seed = new SeedData(setupDb);
        var origin = await seed.AirportAsync("O2C");
        var dest = await seed.AirportAsync("D2D");

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.CreateAsync(Guid.NewGuid(), "XX001", origin.Id, dest.Id, 100m))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_Valid_PersistsFlight()
    {
        await using var setupDb = CreateContext();
        var seed = new SeedData(setupDb);
        var airline = await seed.AirlineAsync("QF", "Qantas");
        var origin = await seed.AirportAsync("SYD");
        var dest = await seed.AirportAsync("MEL");

        await using var db = CreateContext();
        var flight = await MakeSut(db).CreateAsync(airline.Id, "QF001", origin.Id, dest.Id, 150m);

        flight.Id.Should().NotBeEmpty();
        flight.FlightNumber.Should().Be("QF001");
        flight.BasePrice.Should().Be(150m);
    }
}
