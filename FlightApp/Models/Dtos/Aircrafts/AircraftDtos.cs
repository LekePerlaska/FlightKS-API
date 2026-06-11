using FlightKS.Enums;

namespace FlightKS.Models.Dtos.Aircrafts;

public record AircraftDto(
    Guid Id,
    Guid AirlineId,
    string AirlineName,
    string Model,
    string RegistrationNumber,
    int TotalSeats,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record AircraftCreateDto(
    Guid AirlineId,
    string Model,
    string RegistrationNumber,
    int TotalSeats = 0);

public record AircraftUpdateDto(
    Guid? AirlineId,
    string? Model,
    string? RegistrationNumber,
    int? TotalSeats,
    bool? IsActive);

public record SeatAdminDto(
    Guid Id,
    string SeatNumber,
    SeatClass SeatClass,
    bool IsWindow,
    bool IsAisle,
    bool ExtraLegroom);

public record SeatBatchCreateDto(IReadOnlyList<SeatCreateItemDto> Seats);

public record SeatCreateItemDto(
    string SeatNumber,
    SeatClass SeatClass,
    bool IsWindow,
    bool IsAisle,
    bool ExtraLegroom);
