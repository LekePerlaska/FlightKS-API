using FlightKS.Enums;

namespace FlightKS.Models.Dtos.FlightManager;

public record FlightManagerDashboardSummaryDto(
    int TodaySchedules,
    int UpcomingSchedules,
    int DelayedToday,
    int CancelledToday);

public record FlightManagerScheduleListItemDto(
    Guid Id,
    Guid FlightId,
    string FlightNumber,
    string OriginCode,
    string DestinationCode,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    FlightScheduleStatus Status,
    int AvailableSeats,
    string? Gate);

public record FlightManagerPassengerDto(
    Guid PassengerId,
    string FirstName,
    string LastName,
    string? Nationality,
    string? PassportNumber,
    Guid TicketId,
    string TicketNumber,
    TicketStatus TicketStatus,
    string? SeatNumber);

public record FlightManagerSeatDto(
    Guid SeatId,
    Guid? FlightSeatId,
    string SeatNumber,
    SeatClass SeatClass,
    FlightSeatStatus Status,
    decimal Price);

public record FlightManagerSeatStatusUpdateDto(FlightSeatStatus Status);

public record NotifyPassengersDto(string? Title, string Message);
