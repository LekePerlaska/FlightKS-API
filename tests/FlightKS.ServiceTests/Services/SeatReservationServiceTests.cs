using FlightKS.Enums;
using FlightKS.Exceptions;
using FlightKS.Hubs;
using FlightKS.Services;
using FlightKS.ServiceTests.Fixtures;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace FlightKS.ServiceTests.Services;

public class SeatReservationServiceTests(PostgresFixture fixture) : ServiceTestBase(fixture)
{
    // ── hub mock factory ───────────────────────────────────────────────────

    private static (IHubContext<SeatHub> Hub, IClientProxy Proxy) MakeHubMock()
    {
        var proxy = Substitute.For<IClientProxy>();
        proxy.SendCoreAsync(Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
             .Returns(Task.CompletedTask);

        var clients = Substitute.For<IHubClients>();
        clients.Group(Arg.Any<string>()).Returns(proxy);

        var hub = Substitute.For<IHubContext<SeatHub>>();
        hub.Clients.Returns(clients);

        return (hub, proxy);
    }

    private SeatReservationService MakeSut(FlightKS.Data.AppDbContext db, IHubContext<SeatHub>? hub = null) =>
        new(db, hub ?? MakeHubMock().Hub);

    // ── shared seed ────────────────────────────────────────────────────────

    private async Task<SeatScenario> SeedScenarioAsync(
        FlightSeatStatus? existingFlightSeatStatus = null,
        decimal? classPrice = null)
    {
        await using var db = CreateContext();
        var seed = new SeedData(db);

        var user = await seed.UserAsync($"sruser{Guid.NewGuid():N}@test.com");
        var origin = await seed.AirportAsync($"S{Guid.NewGuid().ToString()[..2].ToUpper()}");
        var dest = await seed.AirportAsync($"R{Guid.NewGuid().ToString()[..2].ToUpper()}");
        var airline = await seed.AirlineAsync($"S{Guid.NewGuid().ToString()[..1].ToUpper()}");
        var aircraft = await seed.AircraftAsync(airline.Id);
        var seat = await seed.SeatAsync(aircraft.Id, "12A", SeatClass.Economy);
        var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id);
        var schedule = await seed.ScheduleAsync(flight.Id, aircraft.Id, currentPrice: 150m);

        if (classPrice is { } p)
            await seed.SchedulePriceAsync(schedule.Id, SeatClass.Economy, p);

        var itinResult = await seed.ItineraryAsync(origin.Id, dest.Id, schedule);
        var booking = await seed.BookingAsync(user.Id, itinResult.Itinerary.Id);
        var passenger = await seed.PassengerAsync(booking.Id);

        Guid? existingFlightSeatId = null;
        if (existingFlightSeatStatus is { } status)
        {
            var fs = await seed.FlightSeatAsync(seat.Id, schedule.Id, status, price: 150m);
            existingFlightSeatId = fs.Id;
        }

        return new SeatScenario(
            user.Id, booking.Id, passenger.Id,
            seat.Id, itinResult.Segment.Id, schedule.Id,
            aircraft.Id, existingFlightSeatId);
    }

    private record SeatScenario(
        Guid UserId, Guid BookingId, Guid PassengerId,
        Guid SeatId, Guid SegmentId, Guid ScheduleId,
        Guid AircraftId, Guid? ExistingFlightSeatId);

    // ── ReserveAsync — happy paths ─────────────────────────────────────────

    [Fact]
    public async Task ReserveAsync_NewFlightSeat_CreatesFlightSeatTicketAndReserves()
    {
        var s = await SeedScenarioAsync();
        var (hub, proxy) = MakeHubMock();

        await using var db = CreateContext();
        var result = await new SeatReservationService(db, hub)
            .ReserveAsync(s.BookingId, s.UserId, s.PassengerId, s.SeatId, s.SegmentId);

        result.FlightSeat.Status.Should().Be(FlightSeatStatus.Reserved);
        result.FlightSeat.ReservedUntil.Should().NotBeNull();
        result.Ticket.BookingId.Should().Be(s.BookingId);
        result.Ticket.PassengerId.Should().Be(s.PassengerId);
        result.Ticket.FlightSeatId.Should().Be(result.FlightSeat.Id);

        // Hub must notify
        await proxy.Received(1).SendCoreAsync(
            "SeatReserved",
            Arg.Any<object?[]>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReserveAsync_NewFlightSeat_UsesClassPriceWhenAvailable()
    {
        var s = await SeedScenarioAsync(classPrice: 250m);

        await using var db = CreateContext();
        var result = await MakeSut(db).ReserveAsync(s.BookingId, s.UserId, s.PassengerId, s.SeatId, s.SegmentId);

        result.FlightSeat.Price.Should().Be(250m);
        result.Ticket.Price.Should().Be(250m);
    }

    [Fact]
    public async Task ReserveAsync_NewFlightSeat_FallsBackToSchedulePriceWhenNoClassPrice()
    {
        var s = await SeedScenarioAsync(); // schedule.CurrentPrice = 150m, no class price planted

        await using var db = CreateContext();
        var result = await MakeSut(db).ReserveAsync(s.BookingId, s.UserId, s.PassengerId, s.SeatId, s.SegmentId);

        result.FlightSeat.Price.Should().Be(150m);
    }

    [Fact]
    public async Task ReserveAsync_CustomHoldDuration_SetsReservedUntilCorrectly()
    {
        var s = await SeedScenarioAsync();
        var holdFor = TimeSpan.FromHours(2);

        var before = DateTime.UtcNow;
        await using var db = CreateContext();
        var result = await MakeSut(db).ReserveAsync(s.BookingId, s.UserId, s.PassengerId, s.SeatId, s.SegmentId, holdFor);
        var after = DateTime.UtcNow;

        result.FlightSeat.ReservedUntil.Should().BeCloseTo(before + holdFor, TimeSpan.FromSeconds(5));
        result.FlightSeat.ReservedUntil.Should().BeBefore(after + holdFor + TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ReserveAsync_ExistingAvailableFlightSeat_ReservesIt()
    {
        var s = await SeedScenarioAsync(existingFlightSeatStatus: FlightSeatStatus.Available);

        await using var db = CreateContext();
        var result = await MakeSut(db).ReserveAsync(s.BookingId, s.UserId, s.PassengerId, s.SeatId, s.SegmentId);

        result.FlightSeat.Id.Should().Be(s.ExistingFlightSeatId!.Value);
        result.FlightSeat.Status.Should().Be(FlightSeatStatus.Reserved);
    }

    // ── ReserveAsync — guard paths ─────────────────────────────────────────

    [Fact]
    public async Task ReserveAsync_BookingNotFound_ThrowsNotFoundException()
    {
        var s = await SeedScenarioAsync();
        await using var db = CreateContext();
        await MakeSut(db).Invoking(svc => svc.ReserveAsync(Guid.NewGuid(), s.UserId, s.PassengerId, s.SeatId, s.SegmentId))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ReserveAsync_WrongUser_ThrowsForbiddenException()
    {
        var s = await SeedScenarioAsync();
        await using var db = CreateContext();
        await MakeSut(db).Invoking(svc => svc.ReserveAsync(s.BookingId, Guid.NewGuid(), s.PassengerId, s.SeatId, s.SegmentId))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task ReserveAsync_PassengerNotInBooking_ThrowsNotFoundException()
    {
        var s = await SeedScenarioAsync();
        await using var db = CreateContext();
        await MakeSut(db).Invoking(svc => svc.ReserveAsync(s.BookingId, s.UserId, Guid.NewGuid(), s.SeatId, s.SegmentId))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ReserveAsync_SegmentNotFound_ThrowsNotFoundException()
    {
        var s = await SeedScenarioAsync();
        await using var db = CreateContext();
        await MakeSut(db).Invoking(svc => svc.ReserveAsync(s.BookingId, s.UserId, s.PassengerId, s.SeatId, Guid.NewGuid()))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ReserveAsync_SeatNotOnAircraft_ThrowsNotFoundException()
    {
        var s = await SeedScenarioAsync();
        await using var db = CreateContext();
        await MakeSut(db).Invoking(svc => svc.ReserveAsync(s.BookingId, s.UserId, s.PassengerId, Guid.NewGuid(), s.SegmentId))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData(FlightSeatStatus.Reserved)]
    [InlineData(FlightSeatStatus.Booked)]
    public async Task ReserveAsync_SeatNotAvailable_ThrowsConflictException(FlightSeatStatus blockedStatus)
    {
        var s = await SeedScenarioAsync(existingFlightSeatStatus: blockedStatus);
        await using var db = CreateContext();
        await MakeSut(db).Invoking(svc => svc.ReserveAsync(s.BookingId, s.UserId, s.PassengerId, s.SeatId, s.SegmentId))
            .Should().ThrowAsync<ConflictException>();
    }

    // ── ReleaseAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task ReleaseAsync_OwnedReservedSeat_ReleasesAndNotifiesHub()
    {
        var s = await SeedScenarioAsync();
        var (hub, proxy) = MakeHubMock();

        // First reserve it
        Guid flightSeatId;
        await using (var db = CreateContext())
        {
            var result = await new SeatReservationService(db, hub).ReserveAsync(s.BookingId, s.UserId, s.PassengerId, s.SeatId, s.SegmentId);
            flightSeatId = result.FlightSeat.Id;
        }

        var (hub2, proxy2) = MakeHubMock();
        await using var db2 = CreateContext();
        var released = await new SeatReservationService(db2, hub2).ReleaseAsync(s.BookingId, s.UserId, flightSeatId);

        released.Should().BeTrue();
        var seat = await db2.FlightSeats.FindAsync(flightSeatId);
        seat!.Status.Should().Be(FlightSeatStatus.Available);
        seat.ReservedUntil.Should().BeNull();

        await proxy2.Received(1).SendCoreAsync("SeatReleased", Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReleaseAsync_WrongOwner_ReturnsFalse()
    {
        var s = await SeedScenarioAsync();

        Guid flightSeatId;
        await using (var db = CreateContext())
        {
            var result = await MakeSut(db).ReserveAsync(s.BookingId, s.UserId, s.PassengerId, s.SeatId, s.SegmentId);
            flightSeatId = result.FlightSeat.Id;
        }

        await using var db2 = CreateContext();
        var released = await MakeSut(db2).ReleaseAsync(s.BookingId, Guid.NewGuid(), flightSeatId);
        released.Should().BeFalse();
    }
}
