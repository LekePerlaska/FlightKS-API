using FlightKS.Exceptions;
using FlightKS.Services;
using FlightKS.ServiceTests.Fixtures;

namespace FlightKS.ServiceTests.Services;

public class BookingBaggageServiceTests(PostgresFixture fixture) : ServiceTestBase(fixture)
{
    private static BookingBaggageService MakeSut(FlightKS.Data.AppDbContext db) => new(db);

    private async Task<(Guid UserId, Guid BookingId, Guid PassengerId, Guid BaggageOptionId)> SeedAsync()
    {
        await using var db = CreateContext();
        var seed = new SeedData(db);
        var user = await seed.UserAsync("bb@test.com");
        var origin = await seed.AirportAsync("B1A");
        var dest = await seed.AirportAsync("B1B");
        var airline = await seed.AirlineAsync("BB");
        var aircraft = await seed.AircraftAsync(airline.Id);
        var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id);
        var schedule = await seed.ScheduleAsync(flight.Id, aircraft.Id);
        var itinResult = await seed.ItineraryAsync(origin.Id, dest.Id, schedule);
        var booking = await seed.BookingAsync(user.Id, itinResult.Itinerary.Id);
        var passenger = await seed.PassengerAsync(booking.Id);
        var opt = await seed.BaggageOptionAsync("Hold", 25m);
        return (user.Id, booking.Id, passenger.Id, opt.Id);
    }

    [Fact]
    public async Task AddAsync_WrongOwner_ThrowsForbiddenException()
    {
        var (_, bookingId, passengerId, optId) = await SeedAsync();

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s =>
                s.AddAsync(bookingId, Guid.NewGuid(), passengerId, optId, quantity: 1))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task AddAsync_PassengerNotInBooking_ThrowsNotFoundException()
    {
        var (userId, bookingId, _, optId) = await SeedAsync();

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s =>
                s.AddAsync(bookingId, userId, Guid.NewGuid(), optId, quantity: 1))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddAsync_Valid_PersistsBaggage()
    {
        var (userId, bookingId, passengerId, optId) = await SeedAsync();

        await using var db = CreateContext();
        var result = await MakeSut(db).AddAsync(bookingId, userId, passengerId, optId, quantity: 2);

        result.Quantity.Should().Be(2);
        result.BookingId.Should().Be(bookingId);
    }
}
