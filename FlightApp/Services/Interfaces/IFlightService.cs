using FlightKS.Models.Dtos.Flights;

namespace FlightKS.Services.Interfaces;

public interface IFlightService
{
    Task<IEnumerable<FlightResponseDto>> SearchAsync(FlightSearchQuery query, CancellationToken cancellationToken = default);
    Task<FlightResponseDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
}
