namespace FlightKS.Models.Dtos.Airports;

public record AirportDto(
    Guid Id,
    string Code,
    string Name,
    string City,
    string Country,
    string TimeZone);

public record AirportAdminListItemDto(
    Guid Id,
    string Code,
    string Name,
    string City,
    string Country,
    string TimeZone,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record AirportCreateDto(
    string Code,
    string Name,
    string City,
    string Country,
    string TimeZone);

public record AirportUpdateDto(
    string? Code,
    string? Name,
    string? City,
    string? Country,
    string? TimeZone,
    bool? IsActive);
