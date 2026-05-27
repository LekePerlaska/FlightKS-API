using FlightKS.Enums;
using FlightKS.Models.Entities;

namespace FlightKS.Services.Interfaces;

public record BookingPriceSummary(decimal SeatsTotal, decimal BaggageTotal, decimal PaidTotal, decimal GrandTotal);

public interface IBookingService
{
    Task<Booking> CreateAsync(Guid userId, Guid itineraryId, int passengerCount, CancellationToken cancellationToken = default);
    Task<IEnumerable<Booking>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Booking?> GetByIdAsync(Guid bookingId, Guid? ownerUserId = null, CancellationToken cancellationToken = default);
    Task<Booking?> GetSummaryAsync(Guid bookingId, Guid? ownerUserId, CancellationToken cancellationToken = default);
    Task<Booking?> GetConfirmationAsync(Guid bookingId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<Booking?> UpdateStatusAsync(Guid bookingId, BookingStatus status, CancellationToken cancellationToken = default);
    Task<BookingPriceSummary?> GetPriceSummaryAsync(Guid bookingId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<bool> CancelAsync(Guid bookingId, Guid ownerUserId, CancellationToken cancellationToken = default);
}
