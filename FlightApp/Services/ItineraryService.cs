using FlightKS.Data;
using FlightKS.Enums;
using FlightKS.Exceptions;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace FlightKS.Services;

public class ItineraryService(AppDbContext db) : IItineraryService
{
    public async Task<IEnumerable<Itinerary>> SearchAsync(
        Guid originAirportId,
        Guid destinationAirportId,
        DateOnly departureDate,
        int passengers = 1,
        SeatClass? seatClass = null,
        CancellationToken cancellationToken = default)
    {
        var originTimeZone = await db.Airports.AsNoTracking()
            .Where(a => a.Id == originAirportId)
            .Select(a => a.TimeZone)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "UTC";
        var (dayStart, dayEnd) = GetUtcDateWindow(departureDate, originTimeZone);

        var query = LoadFull(asNoTracking: true)
            .Where(i =>
                i.OriginAirportId == originAirportId &&
                i.DestinationAirportId == destinationAirportId &&
                i.DepartureTime >= dayStart &&
                i.DepartureTime < dayEnd &&
                i.IsActive &&
                i.Segments.All(s => s.FlightSchedule.Flight.IsActive) &&
                i.Segments.All(s => s.FlightSchedule.Status != FlightScheduleStatus.Cancelled));

        if (seatClass is { } cls)
        {
            query = query.Where(i => i.Segments.All(s =>
                db.Seats.Count(st =>
                    st.AircraftId == s.FlightSchedule.AircraftId &&
                    st.SeatClass == cls)
                - db.FlightSeats.Count(fsx =>
                    fsx.FlightScheduleId == s.FlightScheduleId &&
                    fsx.Seat.SeatClass == cls &&
                    fsx.Status != FlightSeatStatus.Available)
                >= passengers));
        }
        else
        {
            query = query.Where(i =>
                !db.ItinerarySegments.Any(s =>
                    s.ItineraryId == i.Id &&
                    s.FlightSchedule.AvailableSeats < passengers));
        }

        return await query
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
                i.Segments.All(s => s.FlightSchedule.Flight.IsActive) &&
                i.Segments.All(s => s.FlightSchedule.Status != FlightScheduleStatus.Cancelled))
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

    public async Task<IEnumerable<Itinerary>> GetAllForAdminAsync(CancellationToken cancellationToken = default) =>
        await LoadFull(asNoTracking: true)
            .IgnoreQueryFilters()
            .OrderByDescending(i => i.DepartureTime)
            .ToListAsync(cancellationToken);

    public async Task<Itinerary> CreateFromSchedulesAsync(List<Guid> flightScheduleIds, CancellationToken cancellationToken = default)
    {
        if (flightScheduleIds is null || flightScheduleIds.Count == 0)
            throw new ValidationException("flightScheduleIds", "An itinerary must contain at least one flight schedule.");
        if (flightScheduleIds.Distinct().Count() != flightScheduleIds.Count)
            throw new ValidationException("flightScheduleIds", "An itinerary cannot use the same flight schedule twice.");

        var schedules = new List<FlightSchedule>(flightScheduleIds.Count);
        foreach (var scheduleId in flightScheduleIds)
        {
            var schedule = await LoadSegmentScheduleAsync(scheduleId, cancellationToken)
                ?? throw new NotFoundException($"Scheduled flight '{scheduleId}' is not available.");
            schedules.Add(schedule);
        }

        var originId = schedules[0].Flight.OriginAirportId;
        var destId = schedules[^1].Flight.DestinationAirportId;

        var candidates = new List<SegmentCandidate>(schedules.Count);
        for (var i = 0; i < schedules.Count; i++)
        {
            int? layover = i < schedules.Count - 1
                ? Math.Max(0, (int)Math.Round((schedules[i + 1].DepartureTime - schedules[i].ArrivalTime).TotalMinutes))
                : null;
            candidates.Add(new SegmentCandidate(i + 1, layover, schedules[i]));
        }

        ValidateSegmentChain(originId, destId, schedules[0].DepartureTime, schedules[^1].ArrivalTime, candidates, enforceTimeWindow: false);

        var itinerary = new Itinerary
        {
            OriginAirportId = originId,
            DestinationAirportId = destId,
            IsActive = true,
        };
        SyncItineraryFromSegments(itinerary, candidates);
        db.Itineraries.Add(itinerary);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var candidate in candidates)
        {
            db.ItinerarySegments.Add(new ItinerarySegment
            {
                ItineraryId = itinerary.Id,
                FlightScheduleId = candidate.Schedule.Id,
                SegmentOrder = candidate.SegmentOrder,
                LayoverMinutesAfterSegment = candidate.LayoverMinutesAfterSegment,
            });
        }
        await db.SaveChangesAsync(cancellationToken);

        return await LoadFull(asNoTracking: true)
            .FirstAsync(i => i.Id == itinerary.Id, cancellationToken);
    }

    public async Task<Itinerary?> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var itinerary = await db.Itineraries.IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (itinerary is null) return null;

        itinerary.IsActive = isActive;
        itinerary.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return await LoadFull(asNoTracking: true)
            .IgnoreQueryFilters()
            .FirstAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<bool> DeleteForAdminAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var itinerary = await db.Itineraries.IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (itinerary is null) return false;

        itinerary.IsActive = false;
        itinerary.DeletedAt = DateTime.UtcNow;
        itinerary.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ItinerarySegment> AddSegmentAsync(Guid itineraryId, Guid scheduleId, int segmentOrder, int? layoverMinutes, CancellationToken cancellationToken = default)
    {
        var itinerary = await LoadEditableItineraryAsync(itineraryId, cancellationToken)
            ?? throw new NotFoundException($"Itinerary '{itineraryId}' not found.");
        var schedule = await LoadSegmentScheduleAsync(scheduleId, cancellationToken)
            ?? throw new NotFoundException($"Scheduled flight '{scheduleId}' not found.");

        ValidateLayover(layoverMinutes);
        var candidates = itinerary.Segments
            .Select(s => new SegmentCandidate(s.SegmentOrder, s.LayoverMinutesAfterSegment, s.FlightSchedule))
            .Append(new SegmentCandidate(segmentOrder, layoverMinutes, schedule))
            .ToList();
        ValidateSegmentChain(
            itinerary.OriginAirportId,
            itinerary.DestinationAirportId,
            itinerary.DepartureTime,
            itinerary.ArrivalTime,
            candidates,
            enforceTimeWindow: false);

        var segment = new ItinerarySegment
        {
            ItineraryId = itineraryId,
            FlightScheduleId = scheduleId,
            SegmentOrder = segmentOrder,
            LayoverMinutesAfterSegment = layoverMinutes,
        };
        db.ItinerarySegments.Add(segment);
        SyncItineraryFromSegments(itinerary, candidates);
        await db.SaveChangesAsync(cancellationToken);

        return await db.ItinerarySegments.AsNoTracking()
            .Include(s => s.FlightSchedule).ThenInclude(fs => fs.Flight).ThenInclude(f => f.Airline)
            .Include(s => s.FlightSchedule).ThenInclude(fs => fs.Flight).ThenInclude(f => f.OriginAirport)
            .Include(s => s.FlightSchedule).ThenInclude(fs => fs.Flight).ThenInclude(f => f.DestinationAirport)
            .FirstAsync(s => s.Id == segment.Id, cancellationToken);
    }

    public async Task<ItinerarySegment?> UpdateSegmentAsync(Guid segmentId, Guid? scheduleId, int? segmentOrder, int? layoverMinutes, CancellationToken cancellationToken = default)
    {
        var segment = await db.ItinerarySegments
            .Include(s => s.Itinerary)
                .ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule)
                        .ThenInclude(fs => fs.Flight)
            .Include(s => s.FlightSchedule)
                .ThenInclude(fs => fs.Flight)
            .FirstOrDefaultAsync(s => s.Id == segmentId, cancellationToken);
        if (segment is null) return null;

        FlightSchedule? replacementSchedule = null;
        if (scheduleId is not null)
        {
            replacementSchedule = await LoadSegmentScheduleAsync(scheduleId.Value, cancellationToken)
                ?? throw new NotFoundException($"Scheduled flight '{scheduleId}' not found.");
        }

        var nextOrder = segmentOrder ?? segment.SegmentOrder;
        var nextLayover = layoverMinutes ?? segment.LayoverMinutesAfterSegment;
        ValidateLayover(nextLayover);

        var candidates = segment.Itinerary.Segments
            .Select(s => s.Id == segmentId
                ? new SegmentCandidate(nextOrder, nextLayover, replacementSchedule ?? s.FlightSchedule)
                : new SegmentCandidate(s.SegmentOrder, s.LayoverMinutesAfterSegment, s.FlightSchedule))
            .ToList();
        ValidateSegmentChain(
            segment.Itinerary.OriginAirportId,
            segment.Itinerary.DestinationAirportId,
            segment.Itinerary.DepartureTime,
            segment.Itinerary.ArrivalTime,
            candidates,
            enforceTimeWindow: false);

        if (scheduleId is not null) segment.FlightScheduleId = scheduleId.Value;
        if (segmentOrder is not null) segment.SegmentOrder = segmentOrder.Value;
        segment.LayoverMinutesAfterSegment = nextLayover;
        segment.UpdatedAt = DateTime.UtcNow;
        SyncItineraryFromSegments(segment.Itinerary, candidates);
        await db.SaveChangesAsync(cancellationToken);

        return await db.ItinerarySegments.AsNoTracking()
            .Include(s => s.FlightSchedule).ThenInclude(fs => fs.Flight).ThenInclude(f => f.Airline)
            .Include(s => s.FlightSchedule).ThenInclude(fs => fs.Flight).ThenInclude(f => f.OriginAirport)
            .Include(s => s.FlightSchedule).ThenInclude(fs => fs.Flight).ThenInclude(f => f.DestinationAirport)
            .FirstAsync(s => s.Id == segmentId, cancellationToken);
    }

    public async Task<bool> DeleteSegmentAsync(Guid segmentId, CancellationToken cancellationToken = default)
    {
        var segment = await db.ItinerarySegments
            .Include(s => s.Itinerary)
                .ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule)
                        .ThenInclude(fs => fs.Flight)
            .FirstOrDefaultAsync(s => s.Id == segmentId, cancellationToken);
        if (segment is null) return false;

        var remainingSegments = segment.Itinerary.Segments
            .Where(s => s.Id != segmentId)
            .Select(s => new SegmentCandidate(s.SegmentOrder, s.LayoverMinutesAfterSegment, s.FlightSchedule))
            .ToList();
        ValidateSegmentChain(
            segment.Itinerary.OriginAirportId,
            segment.Itinerary.DestinationAirportId,
            segment.Itinerary.DepartureTime,
            segment.Itinerary.ArrivalTime,
            remainingSegments,
            enforceTimeWindow: false);
        SyncItineraryFromSegments(segment.Itinerary, remainingSegments);

        db.ItinerarySegments.Remove(segment);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

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
            .Include(i => i.Segments)
                .ThenInclude(s => s.FlightSchedule)
                    .ThenInclude(fs => fs.Prices)
            .AsQueryable();
        return asNoTracking ? q.AsNoTracking() : q;
    }

    private Task<Itinerary?> LoadEditableItineraryAsync(Guid itineraryId, CancellationToken cancellationToken) =>
        db.Itineraries
            .Include(i => i.Segments)
                .ThenInclude(s => s.FlightSchedule)
                    .ThenInclude(fs => fs.Flight)
            .FirstOrDefaultAsync(i => i.Id == itineraryId, cancellationToken);

    private Task<FlightSchedule?> LoadSegmentScheduleAsync(Guid scheduleId, CancellationToken cancellationToken) =>
        db.FlightSchedules.AsNoTracking()
            .Include(s => s.Flight)
            .FirstOrDefaultAsync(s =>
                s.Id == scheduleId &&
                s.Status == FlightScheduleStatus.Scheduled &&
                s.Flight.IsActive,
                cancellationToken);

    private static void ValidateLayover(int? layoverMinutes)
    {
        if (layoverMinutes is < 0)
            throw new ValidationException("layoverMinutes", "Layover minutes cannot be negative.");
    }

    private static void ValidateSegmentChain(
        Guid itineraryOriginId,
        Guid itineraryDestinationId,
        DateTime itineraryDeparture,
        DateTime itineraryArrival,
        IReadOnlyCollection<SegmentCandidate> candidates,
        bool enforceTimeWindow)
    {
        if (candidates.Count == 0) return;

        if (candidates.Any(s => s.SegmentOrder < 1))
            throw new ValidationException("segmentOrder", "Segment order must be at least 1.");

        var ordered = candidates.OrderBy(s => s.SegmentOrder).ToList();
        if (ordered.Select(s => s.SegmentOrder).Distinct().Count() != ordered.Count)
            throw new ValidationException("segmentOrder", "Segment order must be unique within an itinerary.");

        for (var i = 0; i < ordered.Count; i++)
        {
            if (ordered[i].SegmentOrder != i + 1)
                throw new ValidationException("segmentOrder", "Segment order must be continuous starting at 1.");
        }

        if (ordered[0].Schedule.Flight.OriginAirportId != itineraryOriginId)
            throw new ValidationException("segments", "First segment origin must match the itinerary origin.");

        if (ordered[^1].Schedule.Flight.DestinationAirportId != itineraryDestinationId)
            throw new ValidationException("segments", "Last segment destination must match the itinerary destination.");

        if (enforceTimeWindow && (ordered[0].Schedule.DepartureTime < itineraryDeparture || ordered[^1].Schedule.ArrivalTime > itineraryArrival))
            throw new ValidationException("segments", "Segment times must fit inside the itinerary time window.");

        for (var i = 0; i < ordered.Count - 1; i++)
        {
            var current = ordered[i];
            var next = ordered[i + 1];

            if (current.Schedule.Flight.DestinationAirportId != next.Schedule.Flight.OriginAirportId)
                throw new ValidationException("segments", "Segments must connect destination-to-origin in order.");

            if (next.Schedule.DepartureTime < current.Schedule.ArrivalTime)
                throw new ValidationException("segments", "Next segment cannot depart before the previous segment arrives.");

            var actualLayover = (int)Math.Round((next.Schedule.DepartureTime - current.Schedule.ArrivalTime).TotalMinutes);
            if (current.LayoverMinutesAfterSegment is not null && current.LayoverMinutesAfterSegment != actualLayover)
                throw new ValidationException("layoverMinutes", "Layover minutes must match the time between consecutive segments.");
        }

        if (ordered[^1].LayoverMinutesAfterSegment is not null)
            throw new ValidationException("layoverMinutes", "The last segment cannot have a layover after it.");
    }

    private static void SyncItineraryFromSegments(Itinerary itinerary, IReadOnlyCollection<SegmentCandidate> candidates)
    {
        var ordered = candidates.OrderBy(s => s.SegmentOrder).ToList();
        if (ordered.Count == 0) return;

        itinerary.DepartureTime = ordered[0].Schedule.DepartureTime;
        itinerary.ArrivalTime = ordered[^1].Schedule.ArrivalTime;
        itinerary.TotalDurationMinutes = (int)Math.Round((itinerary.ArrivalTime - itinerary.DepartureTime).TotalMinutes);
        itinerary.TotalPrice = ordered.Sum(s => s.Schedule.CurrentPrice);
        itinerary.StopsCount = Math.Max(0, ordered.Count - 1);
        itinerary.UpdatedAt = DateTime.UtcNow;
    }

    private static (DateTime StartUtc, DateTime EndUtc) GetUtcDateWindow(DateOnly date, string timeZone)
    {
        var zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZone) ?? DateTimeZone.Utc;
        var localDate = new LocalDate(date.Year, date.Month, date.Day);
        var start = localDate.AtStartOfDayInZone(zone).ToInstant().ToDateTimeUtc();
        var end = localDate.PlusDays(1).AtStartOfDayInZone(zone).ToInstant().ToDateTimeUtc();
        return (start, end);
    }

    private sealed record SegmentCandidate(
        int SegmentOrder,
        int? LayoverMinutesAfterSegment,
        FlightSchedule Schedule);
}
