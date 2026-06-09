using FlightKS.Exceptions;
using FlightKS.Services;
using FlightKS.ServiceTests.Fixtures;

namespace FlightKS.ServiceTests.Services;

public class AircraftServiceTests(PostgresFixture fixture) : ServiceTestBase(fixture)
{
    private static AircraftService MakeSut(FlightKS.Data.AppDbContext db) => new(db);

    [Fact]
    public async Task CreateAsync_AirlineNotFound_ThrowsNotFoundException()
    {
        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.CreateAsync(Guid.NewGuid(), "B737", "TC-AAA", 180))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_DuplicateRegistration_ThrowsConflictException()
    {
        await using var setupDb = CreateContext();
        var seed = new SeedData(setupDb);
        var airline = await seed.AirlineAsync();
        var existing = await seed.AircraftAsync(airline.Id);

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.CreateAsync(airline.Id, "A320", existing.RegistrationNumber, 150))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateAsync_DuplicateRegistration_IsCaseInsensitive()
    {
        await using var setupDb = CreateContext();
        var seed = new SeedData(setupDb);
        var airline = await seed.AirlineAsync("IC");
        var existing = await seed.AircraftAsync(airline.Id);
        var regLower = existing.RegistrationNumber.ToLower();

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.CreateAsync(airline.Id, "B737", regLower, 120))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateAsync_Valid_PersistsAircraft()
    {
        await using var setupDb = CreateContext();
        var airline = await new SeedData(setupDb).AirlineAsync();

        await using var db = CreateContext();
        var aircraft = await MakeSut(db).CreateAsync(airline.Id, "Boeing 777", "TC-NEW1", 350);

        aircraft.Id.Should().NotBeEmpty();
        aircraft.Model.Should().Be("Boeing 777");
        aircraft.RegistrationNumber.Should().Be("TC-NEW1");
    }

    [Fact]
    public async Task DeleteAsync_HasActiveSchedule_ThrowsBusinessRuleException()
    {
        await using var setupDb = CreateContext();
        var seed = new SeedData(setupDb);
        var airline = await seed.AirlineAsync();
        var aircraft = await seed.AircraftAsync(airline.Id);
        var origin = await seed.AirportAsync("AA1");
        var dest = await seed.AirportAsync("BB2");
        var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id);
        await seed.ScheduleAsync(flight.Id, aircraft.Id);

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.DeleteAsync(aircraft.Id))
            .Should().ThrowAsync<BusinessRuleException>();
    }
}
