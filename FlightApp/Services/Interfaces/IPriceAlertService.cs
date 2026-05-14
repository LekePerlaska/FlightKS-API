using FlightKS.Models.Dtos.Alerts;

namespace FlightKS.Services.Interfaces;

public interface IPriceAlertService
{
    Task<IEnumerable<PriceAlertResponseDto>> GetAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PriceAlertResponseDto> CreateAsync(Guid userId, PriceAlertCreateDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
}
