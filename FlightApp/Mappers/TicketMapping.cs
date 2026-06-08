using FlightKS.Models.Dtos.Tickets;
using FlightKS.Models.Entities;

namespace FlightKS.Mappers;

public static class TicketMapping
{
    public static TicketResponseDto ToResponse(this Ticket t) => new(
        t.Id,
        t.BookingId,
        t.PassengerId,
        $"{t.Passenger.FirstName} {t.Passenger.LastName}",
        t.FlightScheduleId,
        t.FlightSchedule.Flight.FlightNumber,
        t.FlightSchedule.Flight.OriginAirport.Code,
        t.FlightSchedule.Flight.DestinationAirport.Code,
        t.FlightSchedule.DepartureTime,
        t.FlightSchedule.ArrivalTime,
        t.FlightSeatId,
        t.FlightSeat?.Seat.SeatNumber,
        t.TicketNumber,
        t.TicketStatus,
        t.Price,
        t.IssuedAt);
}
