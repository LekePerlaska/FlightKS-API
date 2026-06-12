using FlightKS.Models.Entities;

namespace FlightKS.Services.Interfaces;

public interface IBookingBaggageService
{
    Task<IEnumerable<BookingBaggage>> GetForBookingAsync(Guid bookingId, Guid? ownerUserId = null, CancellationToken cancellationToken = default);

    Task<BookingBaggage> AddAsync(
        Guid bookingId,
        Guid ownerUserId,
        Guid passengerId,
        Guid baggageOptionId,
        int quantity = 1,
        CancellationToken cancellationToken = default);

    Task<BookingBaggage?> UpdateQuantityAsync(
        Guid bookingId,
        Guid bookingBaggageId,
        Guid ownerUserId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(Guid bookingId, Guid bookingBaggageId, Guid ownerUserId, CancellationToken cancellationToken = default);
}
