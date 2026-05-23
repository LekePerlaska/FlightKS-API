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
    int TotalSeats);

public record AircraftUpdateDto(
    Guid? AirlineId,
    string? Model,
    string? RegistrationNumber,
    int? TotalSeats,
    bool? IsActive);
