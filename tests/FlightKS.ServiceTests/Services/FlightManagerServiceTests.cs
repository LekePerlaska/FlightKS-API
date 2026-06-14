using FlightKS.Enums;
using FlightKS.Exceptions;
using FlightKS.Hubs;
using FlightKS.Services;
using FlightKS.Services.Interfaces;
using FlightKS.ServiceTests.Fixtures;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace FlightKS.ServiceTests.Services;

public class FlightManagerServiceTests(PostgresFixture fixture) : ServiceTestBase(fixture)
{
    private static (IHubContext<SeatHub> Hub, IClientProxy Proxy) MakeHub()
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

    private static INotificationService MakeNotifications()
    {
        var n = Substitute.For<INotificationService>();
        n.CreateAsync(default, default!, default!, default!)
         .ReturnsForAnyArgs(Task.FromResult(new FlightKS.Models.Entities.Notification { Title = "", Message = "", Type = "" }));
        return n;
    }

    private FlightManagerService MakeSut(FlightKS.Data.AppDbContext db, IHubContext<SeatHub>? hub = null) =>
        new(db, hub ?? MakeHub().Hub, MakeNotifications());

    private async Task<(Guid ScheduleId, Guid SeatId, Guid TicketId)> SeedScenarioAsync()
    {
        await using var db = CreateContext();
        var seed = new SeedData(db);
        var airline = await seed.AirlineAsync("FM");
        var aircraft = await seed.AircraftAsync(airline.Id);
        var seat = await seed.SeatAsync(aircraft.Id, "1A");
        var origin = await seed.AirportAsync("FM1");
        var dest = await seed.AirportAsync("FM2");
        var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id);
        var schedule = await seed.ScheduleAsync(flight.Id, aircraft.Id);
        var itinResult = await seed.ItineraryAsync(origin.Id, dest.Id, schedule);
        var user = await seed.UserAsync("fm@test.com");
        var booking = await seed.BookingAsync(user.Id, itinResult.Itinerary.Id, status: BookingStatus.Confirmed);
        var passenger = await seed.PassengerAsync(booking.Id);
        var fs = await seed.FlightSeatAsync(seat.Id, schedule.Id, FlightSeatStatus.Booked);
        var ticket = await seed.TicketAsync(booking.Id, passenger.Id, schedule.Id, fs.Id);
        return (schedule.Id, seat.Id, ticket.Id);
    }

    // ── CheckInTicketAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CheckInTicketAsync_IssuedTicket_SetsCheckedIn()
    {
        var (scheduleId, seatId, ticketId) = await SeedScenarioAsync();

        // Change ticket status to Issued (it was set to Issued in SeedData.TicketAsync)
        await using var db = CreateContext();
        var result = await MakeSut(db).CheckInTicketAsync(ticketId);

        result!.TicketStatus.Should().Be(TicketStatus.CheckedIn);
    }

    [Fact]
    public async Task CheckInTicketAsync_AlreadyCheckedIn_ThrowsBusinessRuleException()
    {
        var (_, _, ticketId) = await SeedScenarioAsync();

        await using (var db = CreateContext())
            await MakeSut(db).CheckInTicketAsync(ticketId);

        await using var db2 = CreateContext();
        await MakeSut(db2).Invoking(s => s.CheckInTicketAsync(ticketId))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*already checked in*");
    }

    [Fact]
    public async Task CheckInTicketAsync_CancelledTicket_ThrowsBusinessRuleException()
    {
        await using var setupDb = CreateContext();
        var seed = new SeedData(setupDb);
        var airline = await seed.AirlineAsync("CX");
        var aircraft = await seed.AircraftAsync(airline.Id);
        var origin = await seed.AirportAsync("CX1");
        var dest = await seed.AirportAsync("CX2");
        var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id);
        var schedule = await seed.ScheduleAsync(flight.Id, aircraft.Id);
        var itinResult = await seed.ItineraryAsync(origin.Id, dest.Id, schedule);
        var user = await seed.UserAsync("cx@test.com");
        var booking = await seed.BookingAsync(user.Id, itinResult.Itinerary.Id, status: BookingStatus.Confirmed);
        var passenger = await seed.PassengerAsync(booking.Id);
        var ticket = await seed.TicketAsync(booking.Id, passenger.Id, schedule.Id);

        // Set to Cancelled directly
        ticket.TicketStatus = FlightKS.Enums.TicketStatus.Cancelled;
        await setupDb.SaveChangesAsync();

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.CheckInTicketAsync(ticket.Id))
            .Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CheckInTicketAsync_IssuedTicket_SendsCheckInNotification()
    {
        var (_, _, ticketId) = await SeedScenarioAsync();

        var notifications = Substitute.For<INotificationService>();
        notifications.CreateAsync(default, default!, default!, default!)
            .ReturnsForAnyArgs(Task.FromResult(new FlightKS.Models.Entities.Notification { Title = "", Message = "", Type = "" }));

        await using var db = CreateContext();
        await new FlightManagerService(db, MakeHub().Hub, notifications).CheckInTicketAsync(ticketId);

        await notifications.Received(1).CreateAsync(
            Arg.Any<Guid>(),
            Arg.Is<string>(t => t == "Check-In Confirmed"),
            Arg.Any<string>(),
            Arg.Is<string>(t => t == "check_in_confirmed"),
            Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    // ── SetSeatStatusAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task SetSeatStatusAsync_InvalidStatus_ThrowsValidationException()
    {
        var (scheduleId, seatId, _) = await SeedScenarioAsync();

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.SetSeatStatusAsync(scheduleId, seatId, FlightSeatStatus.Reserved))
            .Should().ThrowAsync<FlightKS.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task SetSeatStatusAsync_BlockedSeat_CreatesFlightSeatAndNotifiesHub()
    {
        await using var setupDb = CreateContext();
        var seed = new SeedData(setupDb);
        var airline = await seed.AirlineAsync("BK");
        var aircraft = await seed.AircraftAsync(airline.Id);
        var seat = await seed.SeatAsync(aircraft.Id, "5C");
        var origin = await seed.AirportAsync("BK1");
        var dest = await seed.AirportAsync("BK2");
        var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id);
        var schedule = await seed.ScheduleAsync(flight.Id, aircraft.Id);

        var (hub, proxy) = MakeHub();
        await using var db = CreateContext();
        var result = await new FlightManagerService(db, hub, MakeNotifications())
            .SetSeatStatusAsync(schedule.Id, seat.Id, FlightSeatStatus.Blocked);

        result!.Status.Should().Be(FlightSeatStatus.Blocked);
        await proxy.Received(1).SendCoreAsync("SeatBlocked", Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
    }
}
