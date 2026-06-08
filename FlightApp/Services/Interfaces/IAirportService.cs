using FlightKS.Models.Entities;

namespace FlightKS.Services.Interfaces;

public interface IAirportService
{
    Task<IEnumerable<Airport>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Airport>> AutocompleteAsync(string query, int limit = 10, CancellationToken cancellationToken = default);
    Task<Airport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Airport>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Airport> CreateAsync(string code, string name, string city, string country, string timeZone, CancellationToken cancellationToken = default);
    Task<Airport?> UpdateAsync(Guid id, string? code, string? name, string? city, string? country, string? timeZone, bool? isActive, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
