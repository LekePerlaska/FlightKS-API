using FlightKS.Models.Entities;

namespace FlightKS.Services.Interfaces;

public interface IBaggageOptionService
{
    Task<IEnumerable<BaggageOption>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BaggageOption?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
