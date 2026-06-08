using FlightKS.Models.Entities;

namespace FlightKS.Services.Interfaces;

public interface IAirlineService
{
    Task<IEnumerable<Airline>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Airline?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Airline>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Airline?> GetByIdForAdminAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Airline> CreateAsync(string code, string name, string country, Guid? logoFileId, CancellationToken cancellationToken = default);
    Task<Airline?> UpdateAsync(Guid id, string? code, string? name, string? country, Guid? logoFileId, bool? isActive, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Airline?> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
}
