using FlightKS.Models.Dtos.Aircrafts;
using FlightKS.Models.Entities;

namespace FlightKS.Services.Interfaces;

public interface IAircraftService
{
    Task<IEnumerable<Aircraft>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Aircraft>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Aircraft?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Aircraft> CreateAsync(Guid airlineId, string model, string registrationNumber, int totalSeats, CancellationToken cancellationToken = default);
    Task<Aircraft?> UpdateAsync(Guid id, Guid? airlineId, string? model, string? registrationNumber, int? totalSeats, bool? isActive, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Seat>> GetSeatsAsync(Guid aircraftId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Seat>> GenerateSeatsAsync(Guid aircraftId, IReadOnlyList<SeatCreateItemDto> seats, CancellationToken cancellationToken = default);
}
