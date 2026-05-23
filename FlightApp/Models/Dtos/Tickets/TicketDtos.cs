using FlightKS.Enums;

namespace FlightKS.Models.Dtos.Tickets;

public record TicketResponseDto(
    Guid Id,
    Guid BookingId,
    Guid PassengerId,
    string PassengerName,
    Guid FlightScheduleId,
    string FlightNumber,
    string OriginCode,
    string DestinationCode,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    Guid? FlightSeatId,
    string? SeatNumber,
    string TicketNumber,
    TicketStatus Status,
    decimal Price,
    DateTime IssuedAt);

public record TicketStatusUpdateDto(TicketStatus Status);
