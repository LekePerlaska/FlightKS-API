using FlightKS.Models.Entities;

namespace FlightKS.Services.Interfaces;

public interface IItineraryService
{
    Task<IEnumerable<Itinerary>> SearchAsync(
        Guid originAirportId,
        Guid destinationAirportId,
        DateOnly departureDate,
        int passengers = 1,
        CancellationToken cancellationToken = default);

    Task<Itinerary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<ItinerarySegment>> GetSegmentsAsync(
        Guid itineraryId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Itinerary>> GetFeaturedAsync(
        int limit = 4,
        CancellationToken cancellationToken = default);
}
