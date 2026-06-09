using FlightKS.Enums;
using FlightKS.Mappers;

namespace FlightKS.UnitTests.Mappers;

public class TicketMappingTests
{
    private static readonly FlightKS.Models.Entities.Airport _lhr = E.Airport("LHR");
    private static readonly FlightKS.Models.Entities.Airport _jfk = E.Airport("JFK");
    private static readonly FlightKS.Models.Entities.Airline _airline = E.Airline();
    private static readonly FlightKS.Models.Entities.Aircraft _aircraft = E.Aircraft(_airline);

    [Fact]
    public void ToResponse_WithSeat_MapsAllFields()
    {
        var flight = E.Flight(_airline, _lhr, _jfk, "BA001");
        var schedule = E.Schedule(flight, _aircraft);
        var booking = E.Booking(Guid.NewGuid());
        var passenger = E.Passenger(booking.Id, "John", "Smith");
        var seat = E.Seat("12A", SeatClass.Economy, isWindow: true);
        var flightSeat = E.FlightSeat(seat, price: 300m);
        var ticket = E.Ticket(booking.Id, passenger.Id, schedule, passenger, "TK-TEST", 300m, flightSeat);

        var dto = ticket.ToResponse();

        dto.Id.Should().Be(ticket.Id);
        dto.BookingId.Should().Be(booking.Id);
        dto.PassengerId.Should().Be(passenger.Id);
        dto.PassengerName.Should().Be("John Smith");
        dto.FlightScheduleId.Should().Be(schedule.Id);
        dto.FlightNumber.Should().Be("BA001");
        dto.OriginCode.Should().Be("LHR");
        dto.DestinationCode.Should().Be("JFK");
        dto.DepartureTime.Should().Be(schedule.DepartureTime);
        dto.ArrivalTime.Should().Be(schedule.ArrivalTime);
        dto.FlightSeatId.Should().Be(flightSeat.Id);
        dto.SeatNumber.Should().Be("12A");
        dto.TicketNumber.Should().Be("TK-TEST");
        dto.Status.Should().Be(TicketStatus.Issued);
        dto.Price.Should().Be(300m);
        dto.IssuedAt.Should().Be(ticket.IssuedAt);
    }

    [Fact]
    public void ToResponse_NoSeat_NullFlightSeatIdAndSeatNumber()
    {
        var flight = E.Flight(_airline, _lhr, _jfk);
        var schedule = E.Schedule(flight, _aircraft);
        var booking = E.Booking(Guid.NewGuid());
        var passenger = E.Passenger(booking.Id, "Ana", "Doe");
        var ticket = E.Ticket(booking.Id, passenger.Id, schedule, passenger, flightSeat: null);

        var dto = ticket.ToResponse();

        dto.FlightSeatId.Should().BeNull();
        dto.SeatNumber.Should().BeNull();
    }
}
