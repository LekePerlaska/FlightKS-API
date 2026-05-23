namespace FlightKS.Models.Dtos.BaggageOptions;

public record BaggageOptionDto(
    Guid Id,
    string Name,
    decimal WeightKg,
    decimal Price,
    string? Description);
