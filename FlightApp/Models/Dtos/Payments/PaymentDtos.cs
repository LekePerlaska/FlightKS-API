using FlightKS.Enums;

namespace FlightKS.Models.Dtos.Payments;

public record PaymentCreateDto(
    Guid BookingId,
    decimal Amount,
    PaymentMethod Method,
    string? TransactionId);

public record PaymentResponseDto(
    Guid Id,
    Guid BookingId,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    string? TransactionId,
    DateTime? PaidAt,
    DateTime CreatedAt);
