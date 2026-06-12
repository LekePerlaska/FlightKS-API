using FlightKS.Exceptions;
using FlightKS.Services;
using FlightKS.ServiceTests.Fixtures;

namespace FlightKS.ServiceTests.Services;

public class PassengerServiceTests(PostgresFixture fixture) : ServiceTestBase(fixture)
{
    private static PassengerService MakeSut(FlightKS.Data.AppDbContext db) => new(db);

    private async Task<(Guid UserId, Guid BookingId)> SeedAsync()
    {
        await using var db = CreateContext();
        var seed = new SeedData(db);
        var user = await seed.UserAsync("ps@test.com");
        var origin = await seed.AirportAsync("PS1");
        var dest = await seed.AirportAsync("PS2");
        var airline = await seed.AirlineAsync("PS");
        var aircraft = await seed.AircraftAsync(airline.Id);
        var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id);
        var schedule = await seed.ScheduleAsync(flight.Id, aircraft.Id);
        var itinResult = await seed.ItineraryAsync(origin.Id, dest.Id, schedule);
        var booking = await seed.BookingAsync(user.Id, itinResult.Itinerary.Id);
        return (user.Id, booking.Id);
    }

    [Fact]
    public async Task AddAsync_WrongOwner_ThrowsForbiddenException()
    {
        var (_, bookingId) = await SeedAsync();

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s =>
                s.AddAsync(bookingId, Guid.NewGuid(), "Jane", "Doe", new DateOnly(1990, 1, 1)))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task AddAsync_Valid_PersistsPassenger()
    {
        var (userId, bookingId) = await SeedAsync();

        await using var db = CreateContext();
        var passenger = await MakeSut(db).AddAsync(bookingId, userId, "Jane", "Doe", new DateOnly(1990, 1, 1));

        passenger.FirstName.Should().Be("Jane");
        passenger.LastName.Should().Be("Doe");
        passenger.BookingId.Should().Be(bookingId);
    }
}
