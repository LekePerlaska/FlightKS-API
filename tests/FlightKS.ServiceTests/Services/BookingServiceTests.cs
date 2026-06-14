using FlightKS.Enums;
using FlightKS.Exceptions;
using FlightKS.Hubs;
using FlightKS.Models.Entities;
using FlightKS.Services;
using FlightKS.Services.Interfaces;
using FlightKS.ServiceTests.Fixtures;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace FlightKS.ServiceTests.Services;

public class BookingServiceTests(PostgresFixture fixture) : ServiceTestBase(fixture)
{
    private static IHubContext<SeatHub> MakeHubMock()
    {
        var proxy = Substitute.For<IClientProxy>();
        proxy.SendCoreAsync(Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
             .Returns(Task.CompletedTask);
        var clients = Substitute.For<IHubClients>();
        clients.Group(Arg.Any<string>()).Returns(proxy);
        var hub = Substitute.For<IHubContext<SeatHub>>();
        hub.Clients.Returns(clients);
        return hub;
    }

    private static BookingService MakeSut(FlightKS.Data.AppDbContext db)
    {
        var notifications = Substitute.For<INotificationService>();
        notifications.CreateAsync(default, default!, default!, default!)
            .ReturnsForAnyArgs(Task.FromResult(new Notification { Title = "", Message = "", Type = "" }));
        return new BookingService(db, notifications, MakeHubMock());
    }

    // ── shared seed helper ─────────────────────────────────────────────────

    private async Task<(
        Guid UserId, Guid OriginId, Guid DestId,
        Guid ItineraryId, Guid ScheduleId
    )> SeedItineraryAsync(
        decimal totalPrice = 200m,
        bool isActive = true,
        decimal schedulePrice = 200m)
    {
        await using var db = CreateContext();
        var seed = new SeedData(db);

        var user = await seed.UserAsync($"bkuser{Guid.NewGuid():N}@test.com");
        var origin = await seed.AirportAsync($"O{Guid.NewGuid().ToString()[..2].ToUpper()}");
        var dest = await seed.AirportAsync($"D{Guid.NewGuid().ToString()[..2].ToUpper()}");
        var airline = await seed.AirlineAsync($"T{Guid.NewGuid().ToString()[..1].ToUpper()}");
        var aircraft = await seed.AircraftAsync(airline.Id);
        var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id);
        var schedule = await seed.ScheduleAsync(flight.Id, aircraft.Id, currentPrice: schedulePrice);
        var itinResult = await seed.ItineraryAsync(origin.Id, dest.Id, schedule, totalPrice: totalPrice);

        if (!isActive)
        {
            itinResult.Itinerary.IsActive = false;
            await db.SaveChangesAsync();
        }

        return (user.Id, origin.Id, dest.Id, itinResult.Itinerary.Id, schedule.Id);
    }

    // ── CreateAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ActiveItinerary_CreatesBookingWithCorrectTotal()
    {
        var (userId, _, _, itinId, _) = await SeedItineraryAsync(totalPrice: 250m);

        await using var db = CreateContext();
        var booking = await MakeSut(db).CreateAsync(userId, itinId, passengerCount: 2);

        booking.Status.Should().Be(BookingStatus.Pending);
        booking.UserId.Should().Be(userId);
        booking.ItineraryId.Should().Be(itinId);
        booking.TotalAmount.Should().Be(500m);   // 250 × 2
        booking.BookingReference.Should().StartWith("BKG-");
        booking.BookingReference.Length.Should().Be(10); // "BKG-" + 6 chars
    }

    [Fact]
    public async Task CreateAsync_WithCabinClassPrice_UsesClassPrice()
    {
        var (userId, _, _, itinId, scheduleId) = await SeedItineraryAsync(totalPrice: 200m, schedulePrice: 200m);

        // plant a Business-class price of 600m on the schedule's segment
        await using (var db = CreateContext())
        {
            var seed = new SeedData(db);
            await seed.SchedulePriceAsync(scheduleId, SeatClass.Business, 600m);
        }

        await using var db2 = CreateContext();
        var booking = await MakeSut(db2).CreateAsync(userId, itinId, 1, SeatClass.Business);

        booking.TotalAmount.Should().Be(600m);
    }

    [Fact]
    public async Task CreateAsync_CabinClassWithNoClassPrice_FallsBackToCurrentPrice()
    {
        // schedulePrice=350m, no First class price planted → falls back to 350
        var (userId, _, _, itinId, _) = await SeedItineraryAsync(totalPrice: 200m, schedulePrice: 350m);

        await using var db = CreateContext();
        var booking = await MakeSut(db).CreateAsync(userId, itinId, 1, SeatClass.First);

        booking.TotalAmount.Should().Be(350m);
    }

    [Fact]
    public async Task CreateAsync_ReleasesSeatsFromUsersOtherPendingBookings()
    {
        Guid userId, itinId, flightSeatId;
        await using (var db = CreateContext())
        {
            var seed = new SeedData(db);
            var user = await seed.UserAsync($"bkuser{Guid.NewGuid():N}@test.com");
            var origin = await seed.AirportAsync($"O{Guid.NewGuid().ToString()[..2].ToUpper()}");
            var dest = await seed.AirportAsync($"D{Guid.NewGuid().ToString()[..2].ToUpper()}");
            var airline = await seed.AirlineAsync($"T{Guid.NewGuid().ToString()[..1].ToUpper()}");
            var aircraft = await seed.AircraftAsync(airline.Id);
            var seat = await seed.SeatAsync(aircraft.Id);
            var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id);
            var schedule = await seed.ScheduleAsync(flight.Id, aircraft.Id);
            var itin = await seed.ItineraryAsync(origin.Id, dest.Id, schedule);

            // An earlier, never-completed pending booking holding a reserved seat.
            var abandoned = await seed.BookingAsync(user.Id, itin.Itinerary.Id, status: BookingStatus.Pending);
            var passenger = await seed.PassengerAsync(abandoned.Id);
            var flightSeat = await seed.FlightSeatAsync(seat.Id, schedule.Id, status: FlightSeatStatus.Reserved);
            await seed.TicketAsync(abandoned.Id, passenger.Id, schedule.Id, flightSeat.Id);

            userId = user.Id;
            itinId = itin.Itinerary.Id;
            flightSeatId = flightSeat.Id;
        }

        // Act: the user starts over, creating a brand new booking.
        await using (var db = CreateContext())
            await MakeSut(db).CreateAsync(userId, itinId, passengerCount: 1);

        // Assert: the abandoned booking's seat is freed and that booking is expired.
        await using (var assertDb = CreateContext())
        {
            var releasedSeat = await assertDb.FlightSeats.AsNoTracking().FirstAsync(fs => fs.Id == flightSeatId);
            releasedSeat.Status.Should().Be(FlightSeatStatus.Available);
            releasedSeat.ReservedUntil.Should().BeNull();

            var expiredCount = await assertDb.Bookings.AsNoTracking()
                .CountAsync(b => b.UserId == userId && b.Status == BookingStatus.Expired);
            expiredCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task CreateAsync_InactiveItinerary_ThrowsNotFoundException()
    {
        var (userId, _, _, itinId, _) = await SeedItineraryAsync(isActive: false);

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.CreateAsync(userId, itinId, 1))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*not found or is no longer active*");
    }

    [Fact]
    public async Task CreateAsync_NonExistentItinerary_ThrowsNotFoundException()
    {
        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.CreateAsync(Guid.NewGuid(), Guid.NewGuid(), 1))
            .Should().ThrowAsync<NotFoundException>();
    }

    // ── GetByIdAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsBooking()
    {
        var (userId, _, _, itinId, _) = await SeedItineraryAsync();

        Guid bookingId;
        await using (var db = CreateContext())
        {
            var b = await MakeSut(db).CreateAsync(userId, itinId, 1);
            bookingId = b.Id;
        }

        await using var db2 = CreateContext();
        var result = await MakeSut(db2).GetByIdAsync(bookingId);
        result.Should().NotBeNull();
        result!.Id.Should().Be(bookingId);
    }

    [Fact]
    public async Task GetByIdAsync_WrongOwner_ReturnsNull()
    {
        var (userId, _, _, itinId, _) = await SeedItineraryAsync();

        Guid bookingId;
        await using (var db = CreateContext())
        {
            var b = await MakeSut(db).CreateAsync(userId, itinId, 1);
            bookingId = b.Id;
        }

        await using var db2 = CreateContext();
        var result = await MakeSut(db2).GetByIdAsync(bookingId, ownerUserId: Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllForAdminAsync_FiltersByLatestPaymentStatusAndDate()
    {
        var targetDate = new DateOnly(2027, 5, 3);
        var targetCreatedAt = new DateTime(2027, 5, 3, 12, 0, 0, DateTimeKind.Utc);

        Guid expectedBookingId;
        await using (var setupDb = CreateContext())
        {
            var seed = new SeedData(setupDb);
            var user = await seed.UserAsync($"admin-bookings-{Guid.NewGuid():N}@test.com");
            var origin = await seed.AirportAsync("AB1");
            var dest = await seed.AirportAsync("AB2");
            var airline = await seed.AirlineAsync("AB");
            var aircraft = await seed.AircraftAsync(airline.Id);
            var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id, "AB123");
            var schedule = await seed.ScheduleAsync(flight.Id, aircraft.Id);
            var itinerary = await seed.ItineraryAsync(origin.Id, dest.Id, schedule);

            var expected = await seed.BookingAsync(user.Id, itinerary.Itinerary.Id);
            expected.CreatedAt = targetCreatedAt;
            await seed.PaymentAsync(expected.Id, expected.TotalAmount, PaymentStatus.Completed);

            var wrongStatus = await seed.BookingAsync(user.Id, itinerary.Itinerary.Id);
            wrongStatus.CreatedAt = targetCreatedAt;
            await seed.PaymentAsync(wrongStatus.Id, wrongStatus.TotalAmount, PaymentStatus.Failed);

            var wrongDate = await seed.BookingAsync(user.Id, itinerary.Itinerary.Id);
            wrongDate.CreatedAt = targetCreatedAt.AddDays(1);
            await seed.PaymentAsync(wrongDate.Id, wrongDate.TotalAmount, PaymentStatus.Completed);

            await setupDb.SaveChangesAsync();
            expectedBookingId = expected.Id;
        }

        await using var db = CreateContext();
        var (items, total) = await MakeSut(db).GetAllForAdminAsync(
            search: null,
            status: null,
            paymentStatus: PaymentStatus.Completed,
            createdDate: targetDate,
            page: 1,
            pageSize: 20);

        total.Should().Be(1);
        items.Should().ContainSingle().Which.Id.Should().Be(expectedBookingId);
    }

    // ── UpdateStatusAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_ExistingBooking_UpdatesStatus()
    {
        var (userId, _, _, itinId, _) = await SeedItineraryAsync();

        Guid bookingId;
        await using (var db = CreateContext())
        {
            var b = await MakeSut(db).CreateAsync(userId, itinId, 1);
            bookingId = b.Id;
        }

        await using var db2 = CreateContext();
        var updated = await MakeSut(db2).UpdateStatusAsync(bookingId, BookingStatus.Cancelled);
        updated!.Status.Should().Be(BookingStatus.Cancelled);
    }

    // ── CancelAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelAsync_OwnedBooking_CancelsAndReturnsTrue()
    {
        var (userId, _, _, itinId, _) = await SeedItineraryAsync();

        Guid bookingId;
        await using (var db = CreateContext())
        {
            var b = await MakeSut(db).CreateAsync(userId, itinId, 1);
            bookingId = b.Id;
        }

        await using var db2 = CreateContext();
        var result = await MakeSut(db2).CancelAsync(bookingId, userId);
        result.Should().BeTrue();

        var booking = await db2.Bookings.FindAsync(bookingId);
        booking!.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task CancelAsync_WrongOwner_ReturnsFalse()
    {
        var (userId, _, _, itinId, _) = await SeedItineraryAsync();

        Guid bookingId;
        await using (var db = CreateContext())
        {
            var b = await MakeSut(db).CreateAsync(userId, itinId, 1);
            bookingId = b.Id;
        }

        await using var db2 = CreateContext();
        var result = await MakeSut(db2).CancelAsync(bookingId, Guid.NewGuid());
        result.Should().BeFalse();
    }
}
