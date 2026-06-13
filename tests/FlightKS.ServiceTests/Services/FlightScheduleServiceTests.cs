using FlightKS.Enums;
using FlightKS.Exceptions;
using FlightKS.Models.Entities;
using FlightKS.Services;
using FlightKS.Services.Interfaces;
using FlightKS.ServiceTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace FlightKS.ServiceTests.Services;

public class FlightScheduleServiceTests(PostgresFixture fixture) : ServiceTestBase(fixture)
{
    private static INotificationService MakeNotifications()
    {
        var n = Substitute.For<INotificationService>();
        n.CreateAsync(default, default!, default!, default!)
         .ReturnsForAnyArgs(Task.FromResult(new Notification { Title = "", Message = "", Type = "" }));
        return n;
    }

    private static FlightScheduleService MakeSut(FlightKS.Data.AppDbContext db, INotificationService? notifications = null) =>
        new(db, notifications ?? MakeNotifications());

    private async Task<(Guid AirlineId, Guid AircraftId, Guid FlightId)> SeedBaseAsync(
        string airlineCode = "TS", string originCode = "TS1", string destCode = "TS2")
    {
        await using var db = CreateContext();
        var seed = new SeedData(db);
        var airline = await seed.AirlineAsync(airlineCode);
        var aircraft = await seed.AircraftAsync(airline.Id);
        var origin = await seed.AirportAsync(originCode);
        var dest = await seed.AirportAsync(destCode);
        var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id);
        return (airline.Id, aircraft.Id, flight.Id);
    }

    [Fact]
    public async Task CreateAsync_AircraftBelongsToDifferentAirline_ThrowsValidationException()
    {
        await using var setupDb = CreateContext();
        var seed = new SeedData(setupDb);
        var airline1 = await seed.AirlineAsync("A1");
        var airline2 = await seed.AirlineAsync("A2");
        var aircraft2 = await seed.AircraftAsync(airline2.Id);      // belongs to airline2
        var origin = await seed.AirportAsync("F1A");
        var dest = await seed.AirportAsync("F1B");
        var flight1 = await seed.FlightAsync(airline1.Id, origin.Id, dest.Id); // belongs to airline1

        var dep = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);
        await using var db = CreateContext();
        await MakeSut(db).Invoking(s =>
                s.CreateAsync(flight1.Id, aircraft2.Id, dep, dep.AddHours(2), null, null))
            .Should().ThrowAsync<FlightKS.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_OverlappingAircraftSchedule_ThrowsConflictException()
    {
        var (_, aircraftId, flightId) = await SeedBaseAsync("FS", "O1S", "D1S");

        var dep = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);
        await using (var db = CreateContext())
            await MakeSut(db).CreateAsync(flightId, aircraftId, dep, dep.AddHours(2), 200m, null);

        // Try to create overlapping schedule (11:00 - 13:00 overlaps 10:00 - 12:00)
        await using var db2 = CreateContext();
        await MakeSut(db2).Invoking(s =>
                s.CreateAsync(flightId, aircraftId, dep.AddHours(1), dep.AddHours(3), 200m, null))
            .Should().ThrowAsync<ConflictException>()
            .WithMessage("*overlapping*");
    }

    [Fact]
    public async Task CreateAsync_Valid_CreatesScheduleAndDirectItinerary()
    {
        var (_, aircraftId, flightId) = await SeedBaseAsync("VS", "O2V", "D2V");

        var dep = new DateTime(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);
        var arr = dep.AddHours(3);

        await using var db = CreateContext();
        var schedule = await MakeSut(db).CreateAsync(flightId, aircraftId, dep, arr, 300m, "B5");

        schedule.Status.Should().Be(FlightScheduleStatus.Scheduled);
        schedule.CurrentPrice.Should().Be(300m);
        schedule.Gate.Should().Be("B5");

        // Auto-created direct itinerary must exist
        var itin = await db.Itineraries.FirstOrDefaultAsync(i =>
            i.Segments.Any(s => s.FlightScheduleId == schedule.Id));
        itin.Should().NotBeNull();
        itin!.DepartureTime.Should().Be(dep);
        itin.ArrivalTime.Should().Be(arr);
        itin.TotalPrice.Should().Be(300m);
    }

    [Fact]
    public async Task DeleteAsync_HasIssuedTickets_ThrowsBusinessRuleException()
    {
        await using var setupDb = CreateContext();
        var seed = new SeedData(setupDb);
        var airline = await seed.AirlineAsync("DL");
        var aircraft = await seed.AircraftAsync(airline.Id);
        var origin = await seed.AirportAsync("D3A");
        var dest = await seed.AirportAsync("D3B");
        var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id);
        var schedule = await seed.ScheduleAsync(flight.Id, aircraft.Id);
        var itinResult = await seed.ItineraryAsync(origin.Id, dest.Id, schedule);
        var user = await seed.UserAsync("ticket@test.com");
        var booking = await seed.BookingAsync(user.Id, itinResult.Itinerary.Id);
        var passenger = await seed.PassengerAsync(booking.Id);
        await seed.TicketAsync(booking.Id, passenger.Id, schedule.Id);

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.DeleteAsync(schedule.Id))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*issued tickets*");
    }

    [Fact]
    public async Task DeleteAsync_NoTickets_SoftDeletesScheduleAndItinerary()
    {
        var (_, aircraftId, flightId) = await SeedBaseAsync("SW", "O4S", "D4S");
        var dep = new DateTime(2027, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        Guid scheduleId;
        Guid itinId;
        await using (var db = CreateContext())
        {
            var s = await MakeSut(db).CreateAsync(flightId, aircraftId, dep, dep.AddHours(2), 200m, null);
            scheduleId = s.Id;
            itinId = (await db.Itineraries.FirstAsync(i => i.Segments.Any(seg => seg.FlightScheduleId == scheduleId))).Id;
        }

        await using var db2 = CreateContext();
        var deleted = await MakeSut(db2).DeleteAsync(scheduleId);

        deleted.Should().BeTrue();
        var schedule = await db2.FlightSchedules.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == scheduleId);
        schedule!.DeletedAt.Should().NotBeNull();

        var itin = await db2.Itineraries.IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == itinId);
        itin!.IsActive.Should().BeFalse();
        itin.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_StatusChangedToDelayed_NotifiesActivePassengers()
    {
        var (_, aircraftId, flightId) = await SeedBaseAsync("ND", "N1D", "N2D");

        Guid scheduleId;
        await using (var db = CreateContext())
        {
            var s = await MakeSut(db).CreateAsync(flightId, aircraftId,
                new DateTime(2027, 3, 1, 9, 0, 0, DateTimeKind.Utc),
                new DateTime(2027, 3, 1, 12, 0, 0, DateTimeKind.Utc), 200m, null);
            scheduleId = s.Id;
        }

        // Plant a ticket holder
        Guid userId;
        await using (var db = CreateContext())
        {
            var seed = new SeedData(db);
            var user = await seed.UserAsync("delay@test.com");
            userId = user.Id;
            var itinResult = await db.Itineraries.FirstAsync(i => i.Segments.Any(seg => seg.FlightScheduleId == scheduleId));
            var booking = await seed.BookingAsync(user.Id, itinResult.Id);
            var passenger = await seed.PassengerAsync(booking.Id);
            var seat = await seed.SeatAsync(aircraftId);
            var fs = await seed.FlightSeatAsync(seat.Id, scheduleId, FlightSeatStatus.Booked);
            await seed.TicketAsync(booking.Id, passenger.Id, scheduleId, fs.Id);
        }

        var notifications = Substitute.For<INotificationService>();
        notifications.CreateAsync(default, default!, default!, default!)
            .ReturnsForAnyArgs(Task.FromResult(new Notification { Title = "", Message = "", Type = "" }));

        await using var db2 = CreateContext();
        await new FlightScheduleService(db2, notifications)
            .UpdateAsync(scheduleId, FlightScheduleStatus.Delayed, null, "Weather", null, null, null, null);

        await notifications.Received(1).CreateAsync(
            Arg.Is(userId),
            Arg.Is<string>(t => t == "Flight Delayed"),
            Arg.Any<string>(),
            Arg.Is<string>(t => t == "flight_delayed"),
            Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_NoPassengers_DoesNotCallNotifications()
    {
        var (_, aircraftId, flightId) = await SeedBaseAsync("NP", "N3P", "N4P");

        Guid scheduleId;
        await using (var db = CreateContext())
        {
            var s = await MakeSut(db).CreateAsync(flightId, aircraftId,
                new DateTime(2027, 4, 1, 9, 0, 0, DateTimeKind.Utc),
                new DateTime(2027, 4, 1, 12, 0, 0, DateTimeKind.Utc), 200m, null);
            scheduleId = s.Id;
        }

        var notifications = Substitute.For<INotificationService>();

        await using var db2 = CreateContext();
        await new FlightScheduleService(db2, notifications)
            .UpdateAsync(scheduleId, FlightScheduleStatus.Cancelled, null, null, null, null, null, null);

        await notifications.DidNotReceive().CreateAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSeatSummaryAsync_CountsAvailableCorrectly()
    {
        await using var setupDb = CreateContext();
        var seed = new SeedData(setupDb);
        var airline = await seed.AirlineAsync("GS");
        var aircraft = await seed.AircraftAsync(airline.Id);
        var seat1 = await seed.SeatAsync(aircraft.Id, "1A", FlightKS.Enums.SeatClass.Economy);
        var seat2 = await seed.SeatAsync(aircraft.Id, "1B", FlightKS.Enums.SeatClass.Economy);
        var origin = await seed.AirportAsync("G5A");
        var dest = await seed.AirportAsync("G5B");
        var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id);
        var schedule = await seed.ScheduleAsync(flight.Id, aircraft.Id);
        // Reserve seat1, leave seat2 available
        await seed.FlightSeatAsync(seat1.Id, schedule.Id, FlightSeatStatus.Reserved);

        await using var db = CreateContext();
        var summary = await MakeSut(db).GetSeatSummaryAsync(schedule.Id);

        summary.Should().NotBeNull();
        summary!.Total.Should().Be(2);
        summary.Available.Should().Be(1);
    }
}
