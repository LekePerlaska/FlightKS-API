using FlightKS.Enums;
using FlightKS.Models.Entities;
using FlightKS.Services;
using FlightKS.Services.Interfaces;
using FlightKS.ServiceTests.Fixtures;
using NSubstitute;

namespace FlightKS.ServiceTests.Services;

public class TicketServiceTests(PostgresFixture fixture) : ServiceTestBase(fixture)
{
    private static INotificationService MakeNotifications()
    {
        var n = Substitute.For<INotificationService>();
        n.CreateAsync(default, default!, default!, default!)
         .ReturnsForAnyArgs(Task.FromResult(new Notification { Title = "", Message = "", Type = "" }));
        return n;
    }

    private static TicketService MakeSut(FlightKS.Data.AppDbContext db, INotificationService? notifications = null) =>
        new(db, notifications ?? MakeNotifications());

    private async Task<(Guid BookingId, Guid TicketId, Guid UserId)> SeedTicketAsync()
    {
        await using var db = CreateContext();
        var seed = new SeedData(db);
        var airline = await seed.AirlineAsync("TK");
        var aircraft = await seed.AircraftAsync(airline.Id);
        var origin = await seed.AirportAsync("TK1");
        var dest = await seed.AirportAsync("TK2");
        var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id);
        var schedule = await seed.ScheduleAsync(flight.Id, aircraft.Id);
        var itinResult = await seed.ItineraryAsync(origin.Id, dest.Id, schedule);
        var user = await seed.UserAsync("ticket@test.com");
        var booking = await seed.BookingAsync(user.Id, itinResult.Itinerary.Id);
        var passenger = await seed.PassengerAsync(booking.Id);
        var ticket = await seed.TicketAsync(booking.Id, passenger.Id, schedule.Id);
        return (booking.Id, ticket.Id, user.Id);
    }

    [Fact]
    public async Task UpdateStatusAsync_ExistingTicket_UpdatesStatus()
    {
        var (_, ticketId, _) = await SeedTicketAsync();

        await using var db = CreateContext();
        var result = await MakeSut(db).UpdateStatusAsync(ticketId, TicketStatus.Used);

        result.Should().NotBeNull();
        result!.TicketStatus.Should().Be(TicketStatus.Used);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonExistentTicket_ReturnsNull()
    {
        await using var db = CreateContext();
        var result = await MakeSut(db).UpdateStatusAsync(Guid.NewGuid(), TicketStatus.Cancelled);
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_StatusChangedToCancelled_SendsNotification()
    {
        var (_, ticketId, userId) = await SeedTicketAsync();

        var notifications = Substitute.For<INotificationService>();
        notifications.CreateAsync(default, default!, default!, default!)
            .ReturnsForAnyArgs(Task.FromResult(new Notification { Title = "", Message = "", Type = "" }));

        await using var db = CreateContext();
        await new TicketService(db, notifications).UpdateStatusAsync(ticketId, TicketStatus.Cancelled);

        await notifications.Received(1).CreateAsync(
            Arg.Is(userId),
            Arg.Is<string>(t => t == "Ticket Cancelled"),
            Arg.Any<string>(),
            Arg.Is<string>(t => t == "ticket_cancelled"),
            Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateStatusAsync_StatusChangedToUsed_DoesNotSendNotification()
    {
        var (_, ticketId, _) = await SeedTicketAsync();

        var notifications = Substitute.For<INotificationService>();

        await using var db = CreateContext();
        await new TicketService(db, notifications).UpdateStatusAsync(ticketId, TicketStatus.Used);

        await notifications.DidNotReceive().CreateAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }
}
