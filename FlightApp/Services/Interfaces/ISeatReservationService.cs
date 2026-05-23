using FlightKS.Models.Entities;

namespace FlightKS.Services.Interfaces;

public record SeatReservationResult(FlightSeat FlightSeat, Ticket Ticket);

public interface ISeatReservationService
{
    Task<IEnumerable<FlightSeat>> GetForBookingAsync(Guid bookingId, Guid? ownerUserId = null, CancellationToken cancellationToken = default);

    Task<SeatReservationResult> ReserveAsync(
        Guid bookingId,
        Guid ownerUserId,
        Guid passengerId,
        Guid flightSeatId,
        TimeSpan? holdFor = null,
        CancellationToken cancellationToken = default);

    Task<bool> ReleaseAsync(Guid bookingId, Guid ownerUserId, Guid flightSeatId, CancellationToken cancellationToken = default);
}
