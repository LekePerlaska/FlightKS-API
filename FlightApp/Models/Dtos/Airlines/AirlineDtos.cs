namespace FlightKS.Models.Dtos.Airlines;

public record AirlineDto(
    Guid Id,
    string Code,
    string Name,
    string Country,
    Guid? LogoFileId);

public record AirlineAdminListItemDto(
    Guid Id,
    string Code,
    string Name,
    string Country,
    Guid? LogoFileId,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record AirlineCreateDto(
    string Code,
    string Name,
    string Country,
    Guid? LogoFileId);

public record AirlineUpdateDto(
    string? Code,
    string? Name,
    string? Country,
    Guid? LogoFileId,
    bool? IsActive);
