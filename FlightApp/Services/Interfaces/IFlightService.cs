using FlightKS.Models.Entities;

namespace FlightKS.Services.Interfaces;

public interface IFlightService
{
    Task<IEnumerable<FlightSchedule>> SearchAsync(
        Guid originAirportId,
        Guid destinationAirportId,
        DateOnly departureDate,
        int passengers = 1,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Airport>> PopularDestinationsAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<FlightSchedule>> FeaturedAsync(int limit = 10, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Flight> Items, int Total)> GetAllForAdminAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Flight?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Flight> CreateAsync(Guid airlineId, string flightNumber, Guid originAirportId, Guid destinationAirportId, decimal basePrice, CancellationToken cancellationToken = default);
    Task<Flight?> UpdateAsync(Guid id, Guid? airlineId, string? flightNumber, Guid? originAirportId, Guid? destinationAirportId, decimal? basePrice, bool? isActive, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
