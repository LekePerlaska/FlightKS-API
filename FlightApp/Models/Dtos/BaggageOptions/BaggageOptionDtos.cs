namespace FlightKS.Models.Dtos.BaggageOptions;

public record BaggageOptionDto(
    Guid Id,
    string Name,
    decimal WeightKg,
    decimal Price,
    string? Description);

public record BaggageOptionAdminListItemDto(
    Guid Id,
    string Name,
    decimal WeightKg,
    decimal Price,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record BaggageOptionCreateDto(
    string Name,
    decimal WeightKg,
    decimal Price,
    string? Description);

public record BaggageOptionUpdateDto(
    string? Name,
    decimal? WeightKg,
    decimal? Price,
    string? Description,
    bool? IsActive);
