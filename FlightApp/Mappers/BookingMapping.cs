using FlightKS.Models.Dtos.BookingBaggage;
using FlightKS.Models.Dtos.Bookings;
using FlightKS.Models.Dtos.Passengers;
using FlightKS.Models.Dtos.Payments;
using FlightKS.Models.Dtos.Tickets;
using FlightKS.Models.Entities;

namespace FlightKS.Mappers;

public static class BookingMapping
{
    public static BookingResponseDto ToResponse(this Booking b) =>
        new(b.Id, b.BookingReference, b.UserId, b.Status, b.TotalAmount, b.CreatedAt, b.UpdatedAt);

    public static BookingListItemDto ToListItem(this Booking b) => new(
        b.Id,
        b.BookingReference,
        b.Status,
        b.TotalAmount,
        b.Passengers.Count,
        b.Tickets.Count,
        b.CreatedAt,
        b.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault()?.PaymentStatus,
        b.Itinerary?.OriginAirport?.Code,
        b.Itinerary?.DestinationAirport?.Code,
        b.Itinerary?.DepartureTime);

    public static BookingSummaryDto ToSummary(this Booking b) => new(
        b.Id,
        b.BookingReference,
        b.Status,
        b.TotalAmount,
        b.CreatedAt,
        [.. b.Passengers.Select(p => p.ToResponse())],
        [.. b.Tickets.Select(t => t.ToResponse())],
        [.. b.BookingBaggage.Select(bb => bb.ToResponse())]);

    public static BookingConfirmationDto ToConfirmation(this Booking b) => new(
        b.Id,
        b.BookingReference,
        b.Status,
        b.TotalAmount,
        b.CreatedAt,
        [.. b.Passengers.Select(p => p.ToResponse())],
        [.. b.Tickets.Select(t => t.ToResponse())],
        [.. b.BookingBaggage.Select(bb => bb.ToResponse())],
        [.. b.Payments.Select(p => p.ToResponse())]);

    public static AdminBookingListItemDto ToAdminListItem(this Booking b)
    {
        var latest = b.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
        return new(
            b.Id,
            b.BookingReference,
            b.Status,
            b.TotalAmount,
            b.Passengers.Count,
            b.Tickets.Count,
            b.CreatedAt,
            latest?.PaymentStatus,
            latest?.Id,
            b.Itinerary?.OriginAirport?.Code,
            b.Itinerary?.DestinationAirport?.Code,
            b.Itinerary?.DepartureTime,
            b.User?.FullName ?? string.Empty,
            b.User?.Email ?? string.Empty);
    }

    public static PassengerResponseDto ToResponse(this Passenger p) => new(
        p.Id, p.BookingId, p.FirstName, p.LastName, p.DateOfBirth,
        p.Gender, p.PassportNumber, p.Nationality, p.CreatedAt);

    public static PaymentResponseDto ToResponse(this Payment p) => new(
        p.Id, p.BookingId, p.Amount, p.PaymentMethod, p.PaymentStatus,
        p.TransactionId, p.PaidAt, p.CreatedAt);

    public static BookingBaggageResponseDto ToResponse(this BookingBaggage bb) => new(
        bb.Id,
        bb.BookingId,
        bb.PassengerId,
        new Models.Dtos.BaggageOptions.BaggageOptionDto(
            bb.BaggageOption.Id,
            bb.BaggageOption.Name,
            bb.BaggageOption.WeightKg,
            bb.BaggageOption.Price,
            bb.BaggageOption.Description),
        bb.Quantity,
        bb.CreatedAt);
}
