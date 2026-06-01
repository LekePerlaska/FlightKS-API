using FlightKS.Models.Entities;

namespace FlightKS.Services.Interfaces;

public interface IBaggageOptionService
{
    Task<IEnumerable<BaggageOption>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BaggageOption?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<BaggageOption>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<BaggageOption?> GetByIdForAdminAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BaggageOption> CreateAsync(string name, decimal weightKg, decimal price, string? description, CancellationToken cancellationToken = default);
    Task<BaggageOption?> UpdateAsync(Guid id, string? name, decimal? weightKg, decimal? price, string? description, bool? isActive, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BaggageOption?> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
}
