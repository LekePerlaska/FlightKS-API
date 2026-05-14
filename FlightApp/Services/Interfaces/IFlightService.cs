using FlightKS.Models.Dtos.Flights;

namespace FlightKS.Services.Interfaces;

public interface IFlightService
{
    Task<IEnumerable<FlightResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FlightResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FlightResponseDto> CreateAsync(FlightCreateDto dto, CancellationToken cancellationToken = default);
    Task<FlightResponseDto?> UpdateAsync(Guid id, FlightUpdateDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
