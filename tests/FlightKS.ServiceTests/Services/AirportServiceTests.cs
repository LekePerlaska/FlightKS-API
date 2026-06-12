using FlightKS.Exceptions;
using FlightKS.Services;
using FlightKS.ServiceTests.Fixtures;

namespace FlightKS.ServiceTests.Services;

public class AirportServiceTests(PostgresFixture fixture) : ServiceTestBase(fixture)
{
    private static AirportService MakeSut(FlightKS.Data.AppDbContext db) => new(db);

    [Fact]
    public async Task CreateAsync_DuplicateCode_ThrowsConflictException()
    {
        await using var setupDb = CreateContext();
        await new SeedData(setupDb).AirportAsync("LHR");

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.CreateAsync("LHR", "Other", "City", "UK", "Europe/London"))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateAsync_CodeUppercased()
    {
        await using var db = CreateContext();
        var airport = await MakeSut(db).CreateAsync("lhr", "Heathrow", "London", "UK", "Europe/London");
        airport.Code.Should().Be("LHR");
    }

    [Fact]
    public async Task DeleteAsync_UsedByFlight_ThrowsBusinessRuleException()
    {
        await using var setupDb = CreateContext();
        var seed = new SeedData(setupDb);
        var origin = await seed.AirportAsync("FR1");
        var dest = await seed.AirportAsync("TO1");
        var airline = await seed.AirlineAsync();
        await seed.FlightAsync(airline.Id, origin.Id, dest.Id);

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.DeleteAsync(origin.Id))
            .Should().ThrowAsync<BusinessRuleException>();
    }
}
