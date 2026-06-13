using FlightKS.Enums;
using FlightKS.Exceptions;
using FlightKS.Models.Entities;
using FlightKS.Services;
using FlightKS.Services.Interfaces;
using FlightKS.ServiceTests.Fixtures;
using NSubstitute;

namespace FlightKS.ServiceTests.Services;

public class BookingServiceTests(PostgresFixture fixture) : ServiceTestBase(fixture)
{
    private static BookingService MakeSut(FlightKS.Data.AppDbContext db)
    {
        var notifications = Substitute.For<INotificationService>();
        notifications.CreateAsync(default, default!, default!, default!)
            .ReturnsForAnyArgs(Task.FromResult(new Notification { Title = "", Message = "", Type = "" }));
        return new BookingService(db, notifications);
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
