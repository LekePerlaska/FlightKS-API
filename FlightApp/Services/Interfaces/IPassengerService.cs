using FlightKS.Models.Entities;

namespace FlightKS.Services.Interfaces;

public interface IPassengerService
{
    Task<IEnumerable<Passenger>> GetForBookingAsync(Guid bookingId, Guid? ownerUserId = null, CancellationToken cancellationToken = default);

    Task<Passenger> AddAsync(
        Guid bookingId,
        Guid ownerUserId,
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        string? gender = null,
        string? passportNumber = null,
        string? nationality = null,
        CancellationToken cancellationToken = default);

    Task<Passenger?> UpdateAsync(
        Guid bookingId,
        Guid passengerId,
        Guid ownerUserId,
        string? firstName = null,
        string? lastName = null,
        DateOnly? dateOfBirth = null,
        string? gender = null,
        string? passportNumber = null,
        string? nationality = null,
        CancellationToken cancellationToken = default);
}
