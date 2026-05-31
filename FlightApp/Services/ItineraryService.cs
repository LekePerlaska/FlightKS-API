using FlightKS.Data;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class ItineraryService(AppDbContext db) : IItineraryService
{
    public async Task<IEnumerable<Itinerary>> SearchAsync(
        Guid originAirportId,
        Guid destinationAirportId,
        DateOnly departureDate,
        int passengers = 1,
        CancellationToken cancellationToken = default)
    {
        var dayStart = departureDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = departureDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        return await LoadFull(asNoTracking: true)
            .Where(i =>
                i.OriginAirportId == originAirportId &&
                i.DestinationAirportId == destinationAirportId &&
                i.DepartureTime >= dayStart &&
                i.DepartureTime <= dayEnd &&
                i.IsActive &&
                i.Segments.All(s => s.FlightSchedule.Flight.IsActive) &&
                !db.ItinerarySegments.Any(s =>
                    s.ItineraryId == i.Id &&
                    s.FlightSchedule.AvailableSeats < passengers))
            .OrderBy(i => i.TotalPrice)
            .ToListAsync(cancellationToken);
    }

    public Task<Itinerary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        LoadFull(asNoTracking: true)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<IEnumerable<Itinerary>> GetFeaturedAsync(
        int limit = 4,
        CancellationToken cancellationToken = default) =>
        await LoadFull(asNoTracking: true)
            .Where(i =>
                i.IsActive &&
                i.DepartureTime > DateTime.UtcNow &&
                i.Segments.All(s => s.FlightSchedule.Flight.IsActive))
            .OrderBy(i => i.TotalPrice)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<ItinerarySegment>> GetSegmentsAsync(
        Guid itineraryId,
        CancellationToken cancellationToken = default) =>
        await db.ItinerarySegments.AsNoTracking()
            .Include(s => s.FlightSchedule).ThenInclude(fs => fs.Flight).ThenInclude(f => f.Airline)
            .Include(s => s.FlightSchedule).ThenInclude(fs => fs.Flight).ThenInclude(f => f.OriginAirport)
            .Include(s => s.FlightSchedule).ThenInclude(fs => fs.Flight).ThenInclude(f => f.DestinationAirport)
            .Where(s => s.ItineraryId == itineraryId)
            .OrderBy(s => s.SegmentOrder)
            .ToListAsync(cancellationToken);

    private IQueryable<Itinerary> LoadFull(bool asNoTracking)
    {
        var q = db.Itineraries
            .Include(i => i.OriginAirport)
            .Include(i => i.DestinationAirport)
            .Include(i => i.Segments.OrderBy(s => s.SegmentOrder))
                .ThenInclude(s => s.FlightSchedule)
                    .ThenInclude(fs => fs.Flight)
                        .ThenInclude(f => f.Airline)
                            .ThenInclude(a => a.LogoFile)
            .Include(i => i.Segments)
                .ThenInclude(s => s.FlightSchedule)
                    .ThenInclude(fs => fs.Flight)
                        .ThenInclude(f => f.OriginAirport)
            .Include(i => i.Segments)
                .ThenInclude(s => s.FlightSchedule)
                    .ThenInclude(fs => fs.Flight)
                        .ThenInclude(f => f.DestinationAirport)
            .AsQueryable();
        return asNoTracking ? q.AsNoTracking() : q;
    }
}
