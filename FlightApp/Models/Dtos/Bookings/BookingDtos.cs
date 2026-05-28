using FlightKS.Enums;
using FlightKS.Models.Dtos.BookingBaggage;
using FlightKS.Models.Dtos.Passengers;
using FlightKS.Models.Dtos.Payments;
using FlightKS.Models.Dtos.Tickets;

namespace FlightKS.Models.Dtos.Bookings;

public record BookingCreateDto(Guid ItineraryId, int PassengerCount, string? CabinClass);

public record BookingResponseDto(
    Guid Id,
    string BookingReference,
    Guid UserId,
    BookingStatus Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record BookingListItemDto(
    Guid Id,
    string BookingReference,
    BookingStatus Status,
    decimal TotalAmount,
    int PassengerCount,
    int TicketCount,
    DateTime CreatedAt,
    PaymentStatus? PaymentStatus,
    string? OriginCode,
    string? DestinationCode,
    DateTime? DepartureTime);

public record BookingSummaryDto(
    Guid Id,
    string BookingReference,
    BookingStatus Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    IReadOnlyList<PassengerResponseDto> Passengers,
    IReadOnlyList<TicketResponseDto> Tickets,
    IReadOnlyList<BookingBaggageResponseDto> Baggage);

public record BookingPriceSummaryDto(
    decimal SeatsTotal,
    decimal BaggageTotal,
    decimal PaidTotal,
    decimal GrandTotal);

public record BookingConfirmationDto(
    Guid Id,
    string BookingReference,
    BookingStatus Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    IReadOnlyList<PassengerResponseDto> Passengers,
    IReadOnlyList<TicketResponseDto> Tickets,
    IReadOnlyList<BookingBaggageResponseDto> Baggage,
    IReadOnlyList<PaymentResponseDto> Payments);
