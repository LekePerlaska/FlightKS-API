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
        var exists = await db.FlightSchedules.AsNoTracking().AnyAsync(s => s.Id == scheduleId, cancellationToken);
        if (!exists) return null;

        var rows = await db.FlightSeats.AsNoTracking()
            .Where(fs => fs.FlightScheduleId == scheduleId)
            .Select(fs => new { fs.Status, fs.Seat.SeatClass })
            .ToListAsync(cancellationToken);

        var available = rows.Count(r => r.Status == FlightSeatStatus.Available);
        var byClass = rows
            .Where(r => r.Status == FlightSeatStatus.Available)
            .GroupBy(r => r.SeatClass)
            .ToDictionary(g => g.Key, g => g.Count());

        return new SeatSummary(rows.Count, available, byClass);
    }

    public async Task<IEnumerable<FlightSeat>> GetSeatsAsync(Guid scheduleId, CancellationToken cancellationToken = default) =>
        await db.FlightSeats.AsNoTracking()
            .Include(fs => fs.Seat)
            .Where(fs => fs.FlightScheduleId == scheduleId)
            .OrderBy(fs => fs.Seat.SeatNumber)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<FlightSchedule>> GetAllForAdminAsync(CancellationToken cancellationToken = default) =>
        await db.FlightSchedules.IgnoreQueryFilters().AsNoTracking()
            .Include(s => s.Flight)
            .OrderByDescending(s => s.DepartureTime)
            .ToListAsync(cancellationToken);

    public async Task<FlightSchedule> CreateAsync(Guid flightId, Guid aircraftId, DateTime departureTime, DateTime arrivalTime, decimal currentPrice, int availableSeats, string? gate, CancellationToken cancellationToken = default)
    {
        if (arrivalTime <= departureTime)
            throw new InvalidOperationException("Arrival time must be after departure time.");

        var flightExists = await db.Flights.AsNoTracking().AnyAsync(f => f.Id == flightId, cancellationToken);
        if (!flightExists) throw new InvalidOperationException($"Flight '{flightId}' not found.");

        var aircraftExists = await db.Aircrafts.AsNoTracking().AnyAsync(a => a.Id == aircraftId, cancellationToken);
        if (!aircraftExists) throw new InvalidOperationException($"Aircraft '{aircraftId}' not found.");

        var schedule = new FlightSchedule
        {
            FlightId = flightId,
            AircraftId = aircraftId,
            DepartureTime = departureTime,
            ArrivalTime = arrivalTime,
            CurrentPrice = currentPrice,
            AvailableSeats = availableSeats,
            Gate = gate,
            Status = FlightScheduleStatus.Scheduled,
        };
        db.FlightSchedules.Add(schedule);
        await db.SaveChangesAsync(cancellationToken);
        return schedule;
    }

    public async Task<FlightSchedule?> UpdateAsync(Guid scheduleId, FlightScheduleStatus? status, string? gate, string? delayReason, DateTime? departureTime, DateTime? arrivalTime, CancellationToken cancellationToken = default)
    {
        var schedule = await db.FlightSchedules.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);
        if (schedule is null) return null;

        if (status is not null) schedule.Status = status.Value;
        if (gate is not null) schedule.Gate = gate;
        if (delayReason is not null) schedule.DelayReason = delayReason;
        if (departureTime is not null) schedule.DepartureTime = departureTime.Value;
        if (arrivalTime is not null) schedule.ArrivalTime = arrivalTime.Value;
        schedule.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return schedule;
    }

    public async Task<bool> DeleteAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await db.FlightSchedules.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);
        if (schedule is null) return false;

        schedule.DeletedAt = DateTime.UtcNow;
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
}
