using FlightKS.Enums;
using FlightKS.Exceptions;
using FlightKS.Models.Entities;
using FlightKS.Services;
using FlightKS.Services.Interfaces;
using FlightKS.ServiceTests.Fixtures;
using NSubstitute;

namespace FlightKS.ServiceTests.Services;

public class PaymentServiceTests(PostgresFixture fixture) : ServiceTestBase(fixture)
{
    private static PaymentService MakeSut(FlightKS.Data.AppDbContext db)
    {
        var notifications = Substitute.For<INotificationService>();
        notifications.CreateAsync(default, default!, default!, default!)
            .ReturnsForAnyArgs(Task.FromResult(new Notification { Title = "", Message = "", Type = "" }));
        return new PaymentService(db, notifications);
    }

    // ── shared seed ─────────────────────────────────────────────────────────

    private async Task<(Guid UserId, Guid BookingId, Guid ScheduleId, Guid FlightSeatId, Guid TicketId)>
        SeedBookingWithReservedSeatAsync(decimal seatPrice = 200m, decimal baggagePrice = 0m)
    {
        await using var db = CreateContext();
        var seed = new SeedData(db);

        var user = await seed.UserAsync();
        var origin = await seed.AirportAsync("LHR");
        var dest = await seed.AirportAsync("DXB");
        var airline = await seed.AirlineAsync();
        var aircraft = await seed.AircraftAsync(airline.Id);
        var seat = await seed.SeatAsync(aircraft.Id);
        var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id);
        var schedule = await seed.ScheduleAsync(flight.Id, aircraft.Id);
        var itinResult = await seed.ItineraryAsync(origin.Id, dest.Id, schedule);
        var booking = await seed.BookingAsync(user.Id, itinResult.Itinerary.Id, totalAmount: seatPrice);
        var passenger = await seed.PassengerAsync(booking.Id);
        var flightSeat = await seed.FlightSeatAsync(seat.Id, schedule.Id, FlightSeatStatus.Reserved, seatPrice);
        var ticket = await seed.TicketAsync(booking.Id, passenger.Id, schedule.Id, flightSeat.Id, seatPrice);

        if (baggagePrice > 0)
        {
            var baggageOpt = await seed.BaggageOptionAsync(price: baggagePrice);
            await seed.BookingBaggageAsync(booking.Id, passenger.Id, baggageOpt.Id, qty: 1);
        }

        return (user.Id, booking.Id, schedule.Id, flightSeat.Id, ticket.Id);
    }

    // ── CreateAsync — happy path ─────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidPayment_CreatesPaymentAndConfirmsBooking()
    {
        var (userId, bookingId, _, flightSeatId, _) = await SeedBookingWithReservedSeatAsync(seatPrice: 300m);

        await using var db = CreateContext();
        var result = await MakeSut(db).CreateAsync(bookingId, userId, 300m, PaymentMethod.Card);

        result.PaymentStatus.Should().Be(PaymentStatus.Completed);
        result.Amount.Should().Be(300m);
        result.BookingId.Should().Be(bookingId);

        var booking = await db.Bookings.FindAsync(bookingId);
        booking!.Status.Should().Be(BookingStatus.Confirmed);

        var seat = await db.FlightSeats.FindAsync(flightSeatId);
        seat!.Status.Should().Be(FlightSeatStatus.Booked);
        seat.ReservedUntil.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WithBaggage_OutstandingIncludesBaggagePrice()
    {
        // seat=200, baggage=50 → grandTotal=250; paying exactly 250 should succeed
        var (userId, bookingId, _, _, _) = await SeedBookingWithReservedSeatAsync(seatPrice: 200m, baggagePrice: 50m);

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.CreateAsync(bookingId, userId, 250m, PaymentMethod.Card))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreateAsync_AlreadyPartiallyPaid_OutstandingReducedByPriorPayments()
    {
        var (userId, bookingId, _, _, _) = await SeedBookingWithReservedSeatAsync(seatPrice: 300m);

        await using (var db = CreateContext())
            await new SeedData(db).PaymentAsync(bookingId, 100m, PaymentStatus.Completed);

        // outstanding = 300 - 100 = 200; paying 200 exactly should succeed
        await using var db2 = CreateContext();
        await MakeSut(db2).Invoking(s => s.CreateAsync(bookingId, userId, 200m, PaymentMethod.Card))
            .Should().NotThrowAsync();
    }

    // ── CreateAsync — access / existence guards ──────────────────────────────

    [Fact]
    public async Task CreateAsync_BookingNotFound_ThrowsNotFoundException()
    {
        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.CreateAsync(Guid.NewGuid(), Guid.NewGuid(), 100m, PaymentMethod.Card))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_WrongOwner_ThrowsForbiddenException()
    {
        var (_, bookingId, _, _, _) = await SeedBookingWithReservedSeatAsync(seatPrice: 200m);

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.CreateAsync(bookingId, Guid.NewGuid(), 200m, PaymentMethod.Card))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CreateAsync_AmountLessThanOutstanding_ThrowsBusinessRuleException()
    {
        var (userId, bookingId, _, _, _) = await SeedBookingWithReservedSeatAsync(seatPrice: 300m);

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.CreateAsync(bookingId, userId, 100m, PaymentMethod.Card))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*outstanding balance*");
    }

    [Fact]
    public async Task CreateAsync_NoTicketsNoBaggage_UsesBookingTotalAmount()
    {
        // Plant a booking with no tickets or baggage; service falls back to TotalAmount
        await using var setupDb = CreateContext();
        var seed = new SeedData(setupDb);
        var user = await seed.UserAsync("noticketsuser@example.com");
        var origin = await seed.AirportAsync("ORD");
        var dest = await seed.AirportAsync("MIA");
        var airline = await seed.AirlineAsync("AA");
        var aircraft = await seed.AircraftAsync(airline.Id);
        var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id, "AA001");
        var schedule = await seed.ScheduleAsync(flight.Id, aircraft.Id);
        var itinResult = await seed.ItineraryAsync(origin.Id, dest.Id, schedule);
        var booking = await seed.BookingAsync(user.Id, itinResult.Itinerary.Id, totalAmount: 500m);

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.CreateAsync(booking.Id, user.Id, 500m, PaymentMethod.Card))
            .Should().NotThrowAsync();
    }

    // ── CreateRefundAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateRefundAsync_CompletedPayment_CreatesRefundAndUpdatesStatuses()
    {
        var (userId, bookingId, _, _, _) = await SeedBookingWithReservedSeatAsync(seatPrice: 200m);

        Guid paymentId;
        await using (var db = CreateContext())
        {
            var createdPayment = await MakeSut(db).CreateAsync(bookingId, userId, 200m, PaymentMethod.Card);
            paymentId = createdPayment.Id;
        }

        await using var db2 = CreateContext();
        var refund = await MakeSut(db2).CreateRefundAsync(paymentId, 200m, "Flight cancelled.");

        refund.Amount.Should().Be(200m);
        refund.Reason.Should().Be("Flight cancelled.");
        refund.RefundStatus.Should().Be(RefundStatus.Completed);

        var fetchedPayment = await db2.Payments.FindAsync(paymentId);
        fetchedPayment!.PaymentStatus.Should().Be(PaymentStatus.Refunded);

        var booking = await db2.Bookings.FindAsync(bookingId);
        booking!.Status.Should().Be(BookingStatus.Refunded);
    }

    [Fact]
    public async Task CreateRefundAsync_PaymentNotFound_ThrowsNotFoundException()
    {
        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.CreateRefundAsync(Guid.NewGuid(), 50m, "reason"))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateRefundAsync_AlreadyRefunded_ThrowsBusinessRuleException()
    {
        var (userId, bookingId, _, _, _) = await SeedBookingWithReservedSeatAsync(seatPrice: 200m);

        Guid paymentId;
        await using (var db = CreateContext())
        {
            var createdPayment = await MakeSut(db).CreateAsync(bookingId, userId, 200m, PaymentMethod.Card);
            paymentId = createdPayment.Id;
        }

        await using (var db2 = CreateContext())
            await MakeSut(db2).CreateRefundAsync(paymentId, 200m, "First refund");

        await using var db3 = CreateContext();
        await MakeSut(db3).Invoking(s => s.CreateRefundAsync(paymentId, 200m, "Second refund"))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*already been refunded*");
    }

    [Fact]
    public async Task CreateRefundAsync_PendingPayment_ThrowsBusinessRuleException()
    {
        await using var setupDb = CreateContext();
        var seed = new SeedData(setupDb);
        var user = await seed.UserAsync("refundpend@example.com");
        var origin = await seed.AirportAsync("A1B");
        var dest = await seed.AirportAsync("C2D");
        var airline = await seed.AirlineAsync("XY");
        var aircraft = await seed.AircraftAsync(airline.Id);
        var flight = await seed.FlightAsync(airline.Id, origin.Id, dest.Id, "XY001");
        var schedule = await seed.ScheduleAsync(flight.Id, aircraft.Id);
        var itinResult = await seed.ItineraryAsync(origin.Id, dest.Id, schedule);
        var booking = await seed.BookingAsync(user.Id, itinResult.Itinerary.Id);
        var pendingPayment = await seed.PaymentAsync(booking.Id, 100m, PaymentStatus.Pending);

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s => s.CreateRefundAsync(pendingPayment.Id, 100m, "reason"))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Only completed payments*");
    }
}
