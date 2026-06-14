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

    public async Task<(IReadOnlyList<Itinerary> Items, int Total)> GetAllForAdminAsync(
        string? search,
        int? stopsCount,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var q = LoadFull(asNoTracking: true).IgnoreQueryFilters().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(i =>
                i.OriginAirport.Code.ToLower().Contains(term) ||
                i.OriginAirport.Name.ToLower().Contains(term) ||
                i.OriginAirport.City.ToLower().Contains(term) ||
                i.DestinationAirport.Code.ToLower().Contains(term) ||
                i.DestinationAirport.Name.ToLower().Contains(term) ||
                i.DestinationAirport.City.ToLower().Contains(term) ||
                i.Segments.Any(s =>
                    s.FlightSchedule.Flight.FlightNumber.ToLower().Contains(term) ||
                    s.FlightSchedule.Flight.Airline.Name.ToLower().Contains(term) ||
                    s.FlightSchedule.Flight.Airline.Code.ToLower().Contains(term)));
        }

        if (stopsCount is not null)
        {
            q = stopsCount >= 2
                ? q.Where(i => i.StopsCount >= stopsCount.Value)
                : q.Where(i => i.StopsCount == stopsCount.Value);
        }

        if (isActive is not null)
            q = q.Where(i => i.IsActive == isActive);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(i => i.DepartureTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<Itinerary> CreateFromSchedulesAsync(List<Guid> flightScheduleIds, CancellationToken cancellationToken = default)
    {
        var schedules = new List<FlightSchedule>(flightScheduleIds.Count);
        foreach (var scheduleId in flightScheduleIds)
        {
            var schedule = await LoadSegmentScheduleAsync(scheduleId, cancellationToken)
                ?? throw new NotFoundException($"Scheduled flight '{scheduleId}' is not available.");
            schedules.Add(schedule);
        }

        var candidates = new List<SegmentCandidate>(schedules.Count);
        for (var i = 0; i < schedules.Count; i++)
        {
            int? layover = i < schedules.Count - 1
                ? Math.Max(0, (int)Math.Round((schedules[i + 1].DepartureTime - schedules[i].ArrivalTime).TotalMinutes))
                : null;
            candidates.Add(new SegmentCandidate(null, i + 1, layover, schedules[i]));
        }

        var derivedCandidates = ValidateAndDeriveSegmentChain(candidates);

        var itinerary = new Itinerary
        {
            IsActive = true,
        };
        SyncItineraryFromSegments(itinerary, derivedCandidates);
        db.Itineraries.Add(itinerary);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var candidate in derivedCandidates)
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
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var itinerary = await LoadEditableItineraryAsync(itineraryId, cancellationToken)
            ?? throw new NotFoundException($"Itinerary '{itineraryId}' not found.");
        var schedule = await LoadSegmentScheduleAsync(scheduleId, cancellationToken)
            ?? throw new NotFoundException($"Scheduled flight '{scheduleId}' not found.");

        var candidates = itinerary.Segments
            .Select(s => new SegmentCandidate(
                s.Id,
                s.SegmentOrder >= segmentOrder ? s.SegmentOrder + 1 : s.SegmentOrder,
                s.LayoverMinutesAfterSegment,
                s.FlightSchedule))
            .Append(new SegmentCandidate(null, segmentOrder, layoverMinutes, schedule))
            .ToList();
        var derivedCandidates = ValidateAndDeriveSegmentChain(candidates);

        await ParkSegmentsForOrderChangesAsync(itinerary, derivedCandidates, cancellationToken);
        ApplyExistingSegmentCandidates(itinerary, derivedCandidates);
        var newCandidate = derivedCandidates.First(c => c.SegmentId is null);

        var segment = new ItinerarySegment
        {
            ItineraryId = itineraryId,
            FlightScheduleId = newCandidate.Schedule.Id,
            SegmentOrder = newCandidate.SegmentOrder,
            LayoverMinutesAfterSegment = newCandidate.LayoverMinutesAfterSegment,
        };
        db.ItinerarySegments.Add(segment);
        SyncItineraryFromSegments(itinerary, derivedCandidates);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await LoadSegmentWithDetailsAsync(segment.Id, cancellationToken);
    }

    public async Task<ItinerarySegment?> UpdateSegmentAsync(Guid segmentId, Guid? scheduleId, int? segmentOrder, int? layoverMinutes, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

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
        if (nextOrder > segment.Itinerary.Segments.Count)
            throw new ValidationException("segmentOrder", "Segment order must be continuous starting at 1.");

        var candidates = segment.Itinerary.Segments
            .Select(s => new SegmentCandidate(
                s.Id,
                segmentOrder is null
                    ? s.SegmentOrder
                    : MoveSegmentOrder(s.SegmentOrder, segment.SegmentOrder, nextOrder),
                s.Id == segmentId ? layoverMinutes : s.LayoverMinutesAfterSegment,
                s.Id == segmentId ? replacementSchedule ?? s.FlightSchedule : s.FlightSchedule))
            .ToList();
        var derivedCandidates = ValidateAndDeriveSegmentChain(candidates);

        await ParkSegmentsForOrderChangesAsync(segment.Itinerary, derivedCandidates, cancellationToken);
        ApplyExistingSegmentCandidates(segment.Itinerary, derivedCandidates);
        SyncItineraryFromSegments(segment.Itinerary, derivedCandidates);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await LoadSegmentWithDetailsAsync(segmentId, cancellationToken);
    }

    public async Task<bool> DeleteSegmentAsync(Guid segmentId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var segment = await db.ItinerarySegments
            .Include(s => s.Itinerary)
                .ThenInclude(i => i.Segments)
                    .ThenInclude(s => s.FlightSchedule)
                        .ThenInclude(fs => fs.Flight)
            .FirstOrDefaultAsync(s => s.Id == segmentId, cancellationToken);
        if (segment is null) return false;

        var remainingSegments = segment.Itinerary.Segments
            .Where(s => s.Id != segmentId)
            .OrderBy(s => s.SegmentOrder)
            .Select((s, index) => new SegmentCandidate(s.Id, index + 1, s.LayoverMinutesAfterSegment, s.FlightSchedule))
            .ToList();
        var derivedCandidates = ValidateAndDeriveSegmentChain(remainingSegments);
        var parked = await ParkSegmentsForOrderChangesAsync(segment.Itinerary, derivedCandidates, cancellationToken);

        if (parked)
        {
            db.ItinerarySegments.Remove(segment);
            await db.SaveChangesAsync(cancellationToken);
        }

        ApplyExistingSegmentCandidates(segment.Itinerary, derivedCandidates);
        SyncItineraryFromSegments(segment.Itinerary, derivedCandidates);

        if (!parked)
            db.ItinerarySegments.Remove(segment);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
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

    private Task<ItinerarySegment> LoadSegmentWithDetailsAsync(Guid segmentId, CancellationToken cancellationToken) =>
        db.ItinerarySegments.AsNoTracking()
            .Include(s => s.FlightSchedule).ThenInclude(fs => fs.Flight).ThenInclude(f => f.Airline)
            .Include(s => s.FlightSchedule).ThenInclude(fs => fs.Flight).ThenInclude(f => f.OriginAirport)
            .Include(s => s.FlightSchedule).ThenInclude(fs => fs.Flight).ThenInclude(f => f.DestinationAirport)
            .FirstAsync(s => s.Id == segmentId, cancellationToken);

    private async Task<bool> ParkSegmentsForOrderChangesAsync(
        Itinerary itinerary,
        IReadOnlyCollection<SegmentCandidate> candidates,
        CancellationToken cancellationToken)
    {
        var finalOrders = candidates
            .Where(c => c.SegmentId is not null)
            .ToDictionary(c => c.SegmentId!.Value, c => c.SegmentOrder);
        var segmentsToPark = itinerary.Segments
            .Where(s => finalOrders.TryGetValue(s.Id, out var finalOrder) && s.SegmentOrder != finalOrder)
            .OrderBy(s => s.SegmentOrder)
            .ToList();

        if (segmentsToPark.Count == 0) return false;

        var temporaryOrder = -1;
        foreach (var segment in segmentsToPark)
        {
            segment.SegmentOrder = temporaryOrder--;
            segment.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ApplyExistingSegmentCandidates(
        Itinerary itinerary,
        IReadOnlyCollection<SegmentCandidate> candidates)
    {
        var bySegmentId = candidates
            .Where(c => c.SegmentId is not null)
            .ToDictionary(c => c.SegmentId!.Value);

        foreach (var segment in itinerary.Segments)
        {
            if (!bySegmentId.TryGetValue(segment.Id, out var candidate)) continue;

            segment.FlightScheduleId = candidate.Schedule.Id;
            segment.SegmentOrder = candidate.SegmentOrder;
            segment.LayoverMinutesAfterSegment = candidate.LayoverMinutesAfterSegment;
            segment.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static int MoveSegmentOrder(int order, int from, int to)
    {
        if (from == to || order == from) return to;
        if (from < to && order > from && order <= to) return order - 1;
        if (from > to && order >= to && order < from) return order + 1;
        return order;
    }

    private static IReadOnlyList<SegmentCandidate> ValidateAndDeriveSegmentChain(IReadOnlyCollection<SegmentCandidate> candidates)
    {
        if (candidates.Count == 0)
            throw new ValidationException("segments", "An itinerary must contain at least one segment.");

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

        for (var i = 0; i < ordered.Count - 1; i++)
        {
            var current = ordered[i];
            var next = ordered[i + 1];

            if (current.Schedule.Flight.DestinationAirportId != next.Schedule.Flight.OriginAirportId)
                throw new ValidationException("segments", "Segments must connect destination-to-origin in order.");

            if (next.Schedule.DepartureTime < current.Schedule.ArrivalTime)
                throw new ValidationException("segments", "Next segment cannot depart before the previous segment arrives.");
        }

        return ordered
            .Select((candidate, index) =>
            {
                var layover = index < ordered.Count - 1
                    ? Math.Max(0, (int)Math.Round((ordered[index + 1].Schedule.DepartureTime - candidate.Schedule.ArrivalTime).TotalMinutes))
                    : (int?)null;
                return candidate with { LayoverMinutesAfterSegment = layover };
            })
            .ToArray();
    }

    private static void SyncItineraryFromSegments(Itinerary itinerary, IReadOnlyCollection<SegmentCandidate> candidates)
    {
        var ordered = candidates.OrderBy(s => s.SegmentOrder).ToList();
        if (ordered.Count == 0) return;

        itinerary.OriginAirportId = ordered[0].Schedule.Flight.OriginAirportId;
        itinerary.DestinationAirportId = ordered[^1].Schedule.Flight.DestinationAirportId;
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
        Guid? SegmentId,
        int SegmentOrder,
        int? LayoverMinutesAfterSegment,
        FlightSchedule Schedule);
}
