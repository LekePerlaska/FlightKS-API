using FlightKS.Models.Dtos.BaggageOptions;

namespace FlightKS.Models.Dtos.BookingBaggage;

public record BookingBaggageCreateDto(
    Guid PassengerId,
    Guid BaggageOptionId,
    int Quantity);

public record BookingBaggageUpdateDto(int Quantity);

public record BookingBaggageResponseDto(
    Guid Id,
    Guid BookingId,
    Guid PassengerId,
    BaggageOptionDto BaggageOption,
    int Quantity,
    DateTime CreatedAt);
