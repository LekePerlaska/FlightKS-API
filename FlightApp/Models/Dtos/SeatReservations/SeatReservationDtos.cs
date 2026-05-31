using FlightKS.Enums;

namespace FlightKS.Models.Dtos.SeatReservations;

public record SeatReservationCreateDto(
    Guid PassengerId,
    Guid SeatId,
    Guid ItinerarySegmentId,
    TimeSpan? HoldFor);

public record SeatReservationResponseDto(
    Guid FlightSeatId,
    Guid SeatId,
    string SeatNumber,
    SeatClass SeatClass,
    Guid PassengerId,
    Guid TicketId,
    string TicketNumber,
    decimal Price,
    FlightSeatStatus Status,
    DateTime? ReservedUntil);
