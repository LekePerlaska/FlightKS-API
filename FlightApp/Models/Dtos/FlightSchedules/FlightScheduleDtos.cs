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
    string AirlineName,
    string AirlineCode,
    string OriginCode,
    string OriginCity,
    string OriginTimeZone,
    string DestinationCode,
    string DestinationCity,
    string DestinationTimeZone,
    Guid? AircraftId,
    string? AircraftModel,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    int DurationMinutes,
    FlightScheduleStatus Status,
    int AvailableSeats,
    decimal CurrentPrice,
    string? Gate,
    string? DelayReason,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<FlightScheduleClassPriceDto> ClassPrices);

public record FlightScheduleClassPriceDto(
    SeatClass SeatClass,
    decimal Price);

public record FlightScheduleCreateDto(
    Guid FlightId,
    Guid AircraftId,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    decimal? CurrentPrice,
    string? Gate,
    IReadOnlyList<FlightScheduleClassPriceDto>? ClassPrices = null);

public record FlightScheduleStatusUpdateDto(
    FlightScheduleStatus? Status,
    string? Gate,
    string? DelayReason,
    DateTime? DepartureTime,
    DateTime? ArrivalTime);

public record FlightScheduleUpdateDto(
    FlightScheduleStatus? Status,
    string? Gate,
    string? DelayReason,
    DateTime? DepartureTime,
    DateTime? ArrivalTime,
    decimal? CurrentPrice,
    int? AvailableSeats,
    IReadOnlyList<FlightScheduleClassPriceDto>? ClassPrices = null);

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

public record ScheduleSeatDto(
    Guid Id,
    string SeatNumber,
    SeatClass SeatClass,
    bool IsWindow,
    bool IsAisle,
    bool ExtraLegroom,
    FlightSeatStatus Status,
    decimal Price);
