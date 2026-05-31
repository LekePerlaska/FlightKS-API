using FlightKS.Data;
using FlightKS.Enums;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class FlightScheduleService(AppDbContext db) : IFlightScheduleService
{
    public Task<FlightSchedule?> GetByIdAsync(Guid scheduleId, CancellationToken cancellationToken = default) =>
        db.FlightSchedules.AsNoTracking()
            .Include(s => s.Flight).ThenInclude(f => f.Airline)
            .Include(s => s.Flight).ThenInclude(f => f.OriginAirport)
            .Include(s => s.Flight).ThenInclude(f => f.DestinationAirport)
            .Include(s => s.Aircraft).ThenInclude(a => a.Airline)
            .FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);

    public async Task<SeatSummary?> GetSeatSummaryAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await db.FlightSchedules.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);
        if (schedule is null) return null;

        var seatClasses = await db.Seats.AsNoTracking()
            .Where(s => s.AircraftId == schedule.AircraftId)
            .Select(s => s.SeatClass)
            .ToListAsync(cancellationToken);

        var total = seatClasses.Count;
        var byClass = seatClasses.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count());
        return new SeatSummary(total, total, byClass);
    }

    public async Task<IEnumerable<Seat>> GetSeatsAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await db.FlightSchedules.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);
        if (schedule is null) return [];

        return await db.Seats.AsNoTracking()
            .Where(s => s.AircraftId == schedule.AircraftId)
            .OrderBy(s => s.SeatNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FlightSchedule>> GetAllForAdminAsync(CancellationToken cancellationToken = default) =>
        await db.FlightSchedules.IgnoreQueryFilters().AsNoTracking()
            .Include(s => s.Flight).ThenInclude(f => f.Airline)
            .Include(s => s.Flight).ThenInclude(f => f.OriginAirport)
            .Include(s => s.Flight).ThenInclude(f => f.DestinationAirport)
            .Include(s => s.Aircraft)
            .Where(s => s.DeletedAt == null)
            .OrderByDescending(s => s.DepartureTime)
            .ToListAsync(cancellationToken);

    public async Task<FlightSchedule> CreateAsync(Guid flightId, Guid aircraftId, DateTime departureTime, DateTime arrivalTime, decimal? currentPrice, string? gate, CancellationToken cancellationToken = default)
    {
        if (arrivalTime <= departureTime)
            throw new InvalidOperationException("Arrival time must be after departure time.");

        // Flight is tracked so the route's typical duration can be seeded from its first schedule.
        var flight = await db.Flights
            .FirstOrDefaultAsync(f => f.Id == flightId && f.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"Active flight '{flightId}' not found.");

        var aircraft = await db.Aircrafts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == aircraftId && a.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"Active aircraft '{aircraftId}' not found.");

        if (aircraft.AirlineId != flight.AirlineId)
            throw new InvalidOperationException("Aircraft must belong to the same airline as the flight.");

        // Price falls back to the flight's base fare; a per-departure override may still be supplied.
        var effectivePrice = currentPrice ?? flight.BasePrice;
        if (effectivePrice <= 0)
            throw new InvalidOperationException("Current price must be greater than zero.");

        await EnsureAircraftIsFreeAsync(aircraftId, departureTime, arrivalTime, excludeScheduleId: null, cancellationToken);

        var schedule = new FlightSchedule
        {
            FlightId = flightId,
            AircraftId = aircraftId,
            DepartureTime = departureTime,
            ArrivalTime = arrivalTime,
            CurrentPrice = effectivePrice,
            AvailableSeats = aircraft.TotalSeats,
            Gate = gate,
            Status = FlightScheduleStatus.Scheduled,
        };
        db.FlightSchedules.Add(schedule);

        // Seed the route's typical duration the first time it is scheduled.
        if (flight.DurationMinutes <= 0)
        {
            flight.DurationMinutes = DurationMinutes(departureTime, arrivalTime);
            flight.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);

        await CreateDirectItineraryAsync(schedule, flight, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return await db.FlightSchedules
            .Include(s => s.Flight).ThenInclude(f => f.Airline)
            .Include(s => s.Flight).ThenInclude(f => f.OriginAirport)
            .Include(s => s.Flight).ThenInclude(f => f.DestinationAirport)
            .Include(s => s.Aircraft)
            .FirstAsync(s => s.Id == schedule.Id, cancellationToken);
    }

    public async Task<FlightSchedule?> UpdateAsync(Guid scheduleId, FlightScheduleStatus? status, string? gate, string? delayReason, DateTime? departureTime, DateTime? arrivalTime, decimal? currentPrice, int? availableSeats, CancellationToken cancellationToken = default)
    {
        var schedule = await db.FlightSchedules.IgnoreQueryFilters()
            .Include(s => s.Flight).ThenInclude(f => f.Airline)
            .Include(s => s.Flight).ThenInclude(f => f.OriginAirport)
            .Include(s => s.Flight).ThenInclude(f => f.DestinationAirport)
            .Include(s => s.Aircraft)
            .FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);
        if (schedule is null) return null;
        if (schedule.DeletedAt is not null)
            throw new InvalidOperationException("Cannot update a deleted schedule.");

        var nextDeparture = departureTime ?? schedule.DepartureTime;
        var nextArrival = arrivalTime ?? schedule.ArrivalTime;
        if (nextArrival <= nextDeparture)
            throw new InvalidOperationException("Arrival time must be after departure time.");

        if (currentPrice is <= 0)
            throw new InvalidOperationException("Current price must be greater than zero.");

        // If the operating window moved, make sure the aircraft isn't now double-booked.
        if (departureTime is not null || arrivalTime is not null)
            await EnsureAircraftIsFreeAsync(schedule.AircraftId, nextDeparture, nextArrival, excludeScheduleId: scheduleId, cancellationToken);

        if (status is not null) schedule.Status = status.Value;
        if (gate is not null) schedule.Gate = gate;
        if (delayReason is not null) schedule.DelayReason = delayReason;
        if (departureTime is not null) schedule.DepartureTime = departureTime.Value;
        if (arrivalTime is not null) schedule.ArrivalTime = arrivalTime.Value;
        if (currentPrice is not null) schedule.CurrentPrice = currentPrice.Value;
        if (availableSeats is not null) schedule.AvailableSeats = availableSeats.Value;
        schedule.UpdatedAt = DateTime.UtcNow;

        await SyncDirectItinerariesAsync(schedule, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return schedule;
    }


    public async Task<bool> DeleteAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await db.FlightSchedules.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);
        if (schedule is null) return false;
        if (schedule.DeletedAt is not null) return false;

        var isInUse = await db.Tickets.AsNoTracking()
            .AnyAsync(t => t.FlightScheduleId == scheduleId, cancellationToken);
        if (isInUse)
            throw new InvalidOperationException("Cannot delete a schedule that has issued tickets.");

        var itineraries = await db.Itineraries
            .IgnoreQueryFilters()
            .Include(i => i.Segments)
            .Include(i => i.Bookings)
            .Where(i => i.Segments.Any(s => s.FlightScheduleId == scheduleId))
            .ToListAsync(cancellationToken);

        if (itineraries.Any(i => i.Segments.Count != 1 || i.Bookings.Count > 0))
            throw new InvalidOperationException("Cannot delete a schedule that is part of a booked or multi-segment itinerary.");

        var now = DateTime.UtcNow;
        foreach (var itinerary in itineraries)
        {
            itinerary.IsActive = false;
            itinerary.DeletedAt = now;
            itinerary.UpdatedAt = now;
        }

        schedule.DeletedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    // TODO: filter by FlightManager assignment once that relationship is modelled. For now returns all schedules.
    public async Task<IEnumerable<FlightSchedule>> GetForFlightManagerAsync(Guid flightManagerUserId, CancellationToken cancellationToken = default) =>
        await db.FlightSchedules.AsNoTracking()
            .Include(s => s.Flight).ThenInclude(f => f.OriginAirport)
            .Include(s => s.Flight).ThenInclude(f => f.DestinationAirport)
            .OrderBy(s => s.DepartureTime)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<(Passenger Passenger, Ticket Ticket)>> GetManifestAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var rows = await db.Tickets.AsNoTracking()
            .Include(t => t.Passenger)
            .Include(t => t.FlightSeat).ThenInclude(fs => fs!.Seat)
            .Where(t => t.FlightScheduleId == scheduleId)
            .ToListAsync(cancellationToken);
        return rows.Select(t => (t.Passenger, t));
    }


    private async Task CreateDirectItineraryAsync(FlightSchedule schedule, Flight flight, CancellationToken cancellationToken)
    {
        var itinerary = new Itinerary
        {
            OriginAirportId = flight.OriginAirportId,
            DestinationAirportId = flight.DestinationAirportId,
            DepartureTime = schedule.DepartureTime,
            ArrivalTime = schedule.ArrivalTime,
            TotalDurationMinutes = DurationMinutes(schedule.DepartureTime, schedule.ArrivalTime),
            TotalPrice = schedule.CurrentPrice,
            StopsCount = 0,
            IsActive = schedule.Status is FlightScheduleStatus.Scheduled or FlightScheduleStatus.Delayed,
        };

        db.Itineraries.Add(itinerary);
        await db.SaveChangesAsync(cancellationToken);

        db.ItinerarySegments.Add(new ItinerarySegment
        {
            ItineraryId = itinerary.Id,
            FlightScheduleId = schedule.Id,
            SegmentOrder = 1,
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SyncDirectItinerariesAsync(FlightSchedule schedule, CancellationToken cancellationToken)
    {
        var itineraries = await db.Itineraries
            .IgnoreQueryFilters()
            .Include(i => i.Segments)
            .Where(i =>
                i.Segments.Count == 1 &&
                i.Segments.Any(s => s.FlightScheduleId == schedule.Id))
            .ToListAsync(cancellationToken);

        foreach (var itinerary in itineraries)
        {
            itinerary.OriginAirportId = schedule.Flight.OriginAirportId;
            itinerary.DestinationAirportId = schedule.Flight.DestinationAirportId;
            itinerary.DepartureTime = schedule.DepartureTime;
            itinerary.ArrivalTime = schedule.ArrivalTime;
            itinerary.TotalDurationMinutes = DurationMinutes(schedule.DepartureTime, schedule.ArrivalTime);
            itinerary.TotalPrice = schedule.CurrentPrice;
            itinerary.StopsCount = 0;
            itinerary.IsActive = schedule.Status is FlightScheduleStatus.Scheduled or FlightScheduleStatus.Delayed;
            itinerary.UpdatedAt = DateTime.UtcNow;
        }
    }

    // An aircraft can only be in one place at a time — reject any overlapping active schedule.
    private async Task EnsureAircraftIsFreeAsync(Guid aircraftId, DateTime departure, DateTime arrival, Guid? excludeScheduleId, CancellationToken cancellationToken)
    {
        var clash = await db.FlightSchedules.AsNoTracking()
            .AnyAsync(s =>
                s.AircraftId == aircraftId &&
                s.Id != excludeScheduleId &&
                s.Status != FlightScheduleStatus.Cancelled &&
                s.DepartureTime < arrival &&
                departure < s.ArrivalTime,
                cancellationToken);
        if (clash)
            throw new InvalidOperationException("This aircraft is already scheduled for an overlapping time window.");
    }

    private static int DurationMinutes(DateTime departure, DateTime arrival) =>
        Math.Max(0, (int)Math.Round((arrival - departure).TotalMinutes));

}
