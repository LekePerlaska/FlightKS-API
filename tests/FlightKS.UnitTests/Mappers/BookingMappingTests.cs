using FlightKS.Enums;
using FlightKS.Mappers;
using FlightKS.Models.Entities;

namespace FlightKS.UnitTests.Mappers;

public class BookingMappingTests
{
    private static readonly Airport _lhr = E.Airport("LHR");
    private static readonly Airport _jfk = E.Airport("JFK");
    private static readonly Airline _airline = E.Airline();
    private static readonly Aircraft _aircraft = E.Aircraft(_airline);

    private FlightSchedule MakeSchedule() =>
        E.Schedule(E.Flight(_airline, _lhr, _jfk), _aircraft);

    private Passenger MakePassenger(Guid bookingId) =>
        E.Passenger(bookingId, "Jane", "Doe");

    private Ticket MakeTicket(Guid bookingId, Passenger passenger) =>
        E.Ticket(bookingId, passenger.Id, MakeSchedule(), passenger);

    [Fact]
    public void ToResponse_MapsAllFields()
    {
        var userId = Guid.NewGuid();
        var b = E.Booking(userId, "REF-001", BookingStatus.Confirmed, 450m);

        var dto = b.ToResponse();

        dto.Id.Should().Be(b.Id);
        dto.BookingReference.Should().Be("REF-001");
        dto.UserId.Should().Be(userId);
        dto.Status.Should().Be(BookingStatus.Confirmed);
        dto.TotalAmount.Should().Be(450m);
        dto.CreatedAt.Should().Be(b.CreatedAt);
        dto.UpdatedAt.Should().Be(b.UpdatedAt);
    }

    [Fact]
    public void ToListItem_NoPaymentsNoItinerary_NullOptionals()
    {
        var b = E.Booking(Guid.NewGuid());

        var dto = b.ToListItem();

        dto.PassengerCount.Should().Be(0);
        dto.TicketCount.Should().Be(0);
        dto.PaymentStatus.Should().BeNull();
        dto.OriginCode.Should().BeNull();
        dto.DestinationCode.Should().BeNull();
        dto.DepartureTime.Should().BeNull();
    }

    [Fact]
    public void ToListItem_WithPaymentAndItinerary_MapsOptionals()
    {
        var b = E.Booking(Guid.NewGuid());
        var payment = E.Payment(b.Id, status: PaymentStatus.Completed);
        b.Payments = [payment];
        var itin = E.Itinerary(_lhr, _jfk);
        b.Itinerary = itin;

        var dto = b.ToListItem();

        dto.PaymentStatus.Should().Be(PaymentStatus.Completed);
        dto.OriginCode.Should().Be("LHR");
        dto.DestinationCode.Should().Be("JFK");
        dto.DepartureTime.Should().Be(itin.DepartureTime);
    }

    [Fact]
    public void ToListItem_MultiplePayments_PicksMostRecent()
    {
        var b = E.Booking(Guid.NewGuid());
        var older = E.Payment(b.Id, status: PaymentStatus.Failed, createdAt: DateTime.UtcNow.AddHours(-2));
        var newer = E.Payment(b.Id, status: PaymentStatus.Completed, createdAt: DateTime.UtcNow);
        b.Payments = [older, newer];

        b.ToListItem().PaymentStatus.Should().Be(PaymentStatus.Completed);
    }

    [Fact]
    public void PassengerToResponse_MapsAllFields()
    {
        var bookingId = Guid.NewGuid();
        var p = E.Passenger(bookingId, "Alice", "Wonder");
        p.Gender = "Female";
        p.PassportNumber = "XY999";
        p.Nationality = "British";

        var dto = p.ToResponse();

        dto.Id.Should().Be(p.Id);
        dto.BookingId.Should().Be(bookingId);
        dto.FirstName.Should().Be("Alice");
        dto.LastName.Should().Be("Wonder");
        dto.DateOfBirth.Should().Be(new DateOnly(1990, 5, 15));
        dto.Gender.Should().Be("Female");
        dto.PassportNumber.Should().Be("XY999");
        dto.Nationality.Should().Be("British");
        dto.CreatedAt.Should().Be(p.CreatedAt);
    }

    [Fact]
    public void PaymentToResponse_MapsAllFields()
    {
        var bookingId = Guid.NewGuid();
        var p = E.Payment(bookingId, 350m, PaymentStatus.Completed);
        p.TransactionId = "TXN-123";

        var dto = p.ToResponse();

        dto.Id.Should().Be(p.Id);
        dto.BookingId.Should().Be(bookingId);
        dto.Amount.Should().Be(350m);
        dto.Method.Should().Be(PaymentMethod.Card);
        dto.Status.Should().Be(PaymentStatus.Completed);
        dto.TransactionId.Should().Be("TXN-123");
        dto.PaidAt.Should().Be(p.PaidAt);
        dto.CreatedAt.Should().Be(p.CreatedAt);
    }

    [Fact]
    public void BookingBaggageToResponse_MapsAllFields()
    {
        var bookingId = Guid.NewGuid();
        var passengerId = Guid.NewGuid();
        var opt = E.BaggageOpt("Hold 23kg", 23m, 45m);
        var bb = E.BookingBaggage(bookingId, passengerId, opt, qty: 2);

        var dto = bb.ToResponse();

        dto.Id.Should().Be(bb.Id);
        dto.BookingId.Should().Be(bookingId);
        dto.PassengerId.Should().Be(passengerId);
        dto.BaggageOption.Name.Should().Be("Hold 23kg");
        dto.BaggageOption.WeightKg.Should().Be(23m);
        dto.BaggageOption.Price.Should().Be(45m);
        dto.Quantity.Should().Be(2);
        dto.CreatedAt.Should().Be(bb.CreatedAt);
    }

    [Fact]
    public void ToSummary_MapsCollections()
    {
        var b = E.Booking(Guid.NewGuid());
        var passenger = MakePassenger(b.Id);
        var ticket = MakeTicket(b.Id, passenger);
        var opt = E.BaggageOpt();
        var baggage = E.BookingBaggage(b.Id, passenger.Id, opt);
        b.Passengers = [passenger];
        b.Tickets = [ticket];
        b.BookingBaggage = [baggage];

        var dto = b.ToSummary();

        dto.Id.Should().Be(b.Id);
        dto.Passengers.Should().HaveCount(1);
        dto.Tickets.Should().HaveCount(1);
        dto.Baggage.Should().HaveCount(1);
    }

    [Fact]
    public void ToConfirmation_IncludesPayments()
    {
        var b = E.Booking(Guid.NewGuid());
        var passenger = MakePassenger(b.Id);
        var ticket = MakeTicket(b.Id, passenger);
        var opt = E.BaggageOpt();
        var baggage = E.BookingBaggage(b.Id, passenger.Id, opt);
        var payment = E.Payment(b.Id);
        b.Passengers = [passenger];
        b.Tickets = [ticket];
        b.BookingBaggage = [baggage];
        b.Payments = [payment];

        var dto = b.ToConfirmation();

        dto.Payments.Should().HaveCount(1);
        dto.Payments[0].Amount.Should().Be(payment.Amount);
    }

    [Fact]
    public void ToAdminListItem_WithUser_MapsUserFields()
    {
        var b = E.Booking(Guid.NewGuid());
        b.User = E.User("admin@example.com", "Alice Admin");

        var dto = b.ToAdminListItem();

        dto.UserFullName.Should().Be("Alice Admin");
        dto.UserEmail.Should().Be("admin@example.com");
    }

    [Fact]
    public void ToAdminListItem_NullUser_UsesEmptyStrings()
    {
        var b = E.Booking(Guid.NewGuid());
        b.User = null!;

        var dto = b.ToAdminListItem();

        dto.UserFullName.Should().Be(string.Empty);
        dto.UserEmail.Should().Be(string.Empty);
    }

    [Fact]
    public void ToAdminListItem_WithLatestPayment_MapsPaymentFields()
    {
        var b = E.Booking(Guid.NewGuid());
        b.User = E.User();
        var older = E.Payment(b.Id, status: PaymentStatus.Failed, createdAt: DateTime.UtcNow.AddHours(-3));
        var newer = E.Payment(b.Id, status: PaymentStatus.Completed, createdAt: DateTime.UtcNow);
        b.Payments = [older, newer];

        var dto = b.ToAdminListItem();

        dto.PaymentStatus.Should().Be(PaymentStatus.Completed);
        dto.LatestPaymentId.Should().Be(newer.Id);
    }
}
