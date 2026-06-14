using FlightKS.Enums;
using FlightKS.Models.Entities;

namespace FlightKS.Services.Interfaces;

public interface IItineraryService
{
    Task<IEnumerable<Itinerary>> SearchAsync(
        Guid originAirportId,
        Guid destinationAirportId,
        DateOnly departureDate,
        int passengers = 1,
        SeatClass? seatClass = null,
        CancellationToken cancellationToken = default);

    Task<Itinerary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<ItinerarySegment>> GetSegmentsAsync(
        Guid itineraryId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Itinerary>> GetFeaturedAsync(
        int limit = 4,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Itinerary> Items, int Total)> GetAllForAdminAsync(
        string? search,
        int? stopsCount,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<Itinerary> CreateFromSchedulesAsync(List<Guid> flightScheduleIds, CancellationToken cancellationToken = default);
    Task<Itinerary?> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
    Task<bool> DeleteForAdminAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ItinerarySegment> AddSegmentAsync(Guid itineraryId, Guid scheduleId, int segmentOrder, int? layoverMinutes, CancellationToken cancellationToken = default);
    Task<ItinerarySegment?> UpdateSegmentAsync(Guid segmentId, Guid? scheduleId, int? segmentOrder, int? layoverMinutes, CancellationToken cancellationToken = default);
    Task<bool> DeleteSegmentAsync(Guid segmentId, CancellationToken cancellationToken = default);
}
