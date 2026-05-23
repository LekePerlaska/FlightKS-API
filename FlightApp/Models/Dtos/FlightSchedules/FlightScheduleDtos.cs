using FlightKS.Enums;
using FlightKS.Models.Dtos.Aircrafts;
using FlightKS.Models.Dtos.Airlines;
using FlightKS.Models.Dtos.Airports;

namespace FlightKS.Models.Dtos.FlightSchedules;

public record FlightScheduleDetailDto(
    Guid Id,
    Guid FlightId,
    string FlightNumber,
    AirlineDto Airline,
    AirportDto Origin,
    AirportDto Destination,
    AircraftDto Aircraft,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    int DurationMinutes,
    FlightScheduleStatus Status,
    int AvailableSeats,
    decimal CurrentPrice,
    string? Gate,
    string? DelayReason);

public record FlightScheduleAdminListItemDto(
    Guid Id,
    Guid FlightId,
    string FlightNumber,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    FlightScheduleStatus Status,
    int AvailableSeats,
    decimal CurrentPrice,
    string? Gate);

public record FlightScheduleCreateDto(
    Guid FlightId,
    Guid AircraftId,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    decimal CurrentPrice,
    int AvailableSeats,
    string? Gate);

public record FlightScheduleStatusUpdateDto(
    FlightScheduleStatus? Status,
    string? Gate,
    string? DelayReason,
    DateTime? DepartureTime,
    DateTime? ArrivalTime);

public record SeatSummaryDto(
    int Total,
    int Available,
    Dictionary<SeatClass, int> AvailableByClass);

public record FlightSeatDto(
    Guid Id,
    Guid SeatId,
    string SeatNumber,
    SeatClass SeatClass,
    bool IsWindow,
    bool IsAisle,
    bool ExtraLegroom,
    FlightSeatStatus Status,
    decimal Price,
    DateTime? ReservedUntil);
