using FlightKS.Data;
using FlightKS.Enums;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class FlightService(AppDbContext db) : IFlightService
{
    public async Task<IEnumerable<FlightSchedule>> SearchAsync(
        Guid originAirportId,
        Guid destinationAirportId,
        DateOnly departureDate,
        int passengers = 1,
        CancellationToken cancellationToken = default)
    {
        var dayStart = departureDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = departureDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        return await db.FlightSchedules.AsNoTracking()
            .Include(s => s.Flight).ThenInclude(f => f.Airline)
            .Include(s => s.Flight).ThenInclude(f => f.OriginAirport)
            .Include(s => s.Flight).ThenInclude(f => f.DestinationAirport)
            .Include(s => s.Aircraft)
            .Where(s =>
                s.Flight.OriginAirportId == originAirportId &&
                s.Flight.DestinationAirportId == destinationAirportId &&
                s.DepartureTime >= dayStart &&
                s.DepartureTime <= dayEnd &&
                s.Status == FlightScheduleStatus.Scheduled &&
                s.AvailableSeats >= passengers)
            .OrderBy(s => s.DepartureTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Airport>> PopularDestinationsAsync(int limit = 10, CancellationToken cancellationToken = default) =>
        await db.Tickets.AsNoTracking()
            .GroupBy(t => t.FlightSchedule.Flight.DestinationAirport)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<FlightSchedule>> FeaturedAsync(int limit = 10, CancellationToken cancellationToken = default) =>
        await db.FlightSchedules.AsNoTracking()
            .Include(s => s.Flight).ThenInclude(f => f.Airline)
            .Include(s => s.Flight).ThenInclude(f => f.OriginAirport)
            .Include(s => s.Flight).ThenInclude(f => f.DestinationAirport)
            .Where(s => s.Status == FlightScheduleStatus.Scheduled && s.DepartureTime > DateTime.UtcNow)
            .OrderBy(s => s.CurrentPrice)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Flight>> GetAllForAdminAsync(CancellationToken cancellationToken = default) =>
        await db.Flights.IgnoreQueryFilters().AsNoTracking()
            .Include(f => f.Airline)
            .Include(f => f.OriginAirport)
            .Include(f => f.DestinationAirport)
            .OrderBy(f => f.Airline.Name).ThenBy(f => f.FlightNumber)
            .ToListAsync(cancellationToken);

    public Task<Flight?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Flights.AsNoTracking()
            .Include(f => f.Airline)
            .Include(f => f.OriginAirport)
            .Include(f => f.DestinationAirport)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<Flight> CreateAsync(Guid airlineId, string flightNumber, Guid originAirportId, Guid destinationAirportId, decimal basePrice, int durationMinutes, CancellationToken cancellationToken = default)
    {
        if (originAirportId == destinationAirportId)
            throw new InvalidOperationException("Origin and destination airports must differ.");

        var dup = await db.Flights.AsNoTracking()
            .AnyAsync(f => f.AirlineId == airlineId && f.FlightNumber == flightNumber, cancellationToken);
        if (dup) throw new InvalidOperationException($"Flight number '{flightNumber}' already exists for this airline.");

        var flight = new Flight
        {
            AirlineId = airlineId,
            FlightNumber = flightNumber,
            OriginAirportId = originAirportId,
            DestinationAirportId = destinationAirportId,
            BasePrice = basePrice,
            DurationMinutes = durationMinutes,
        };
        db.Flights.Add(flight);
        await db.SaveChangesAsync(cancellationToken);

        await db.Entry(flight).Reference(f => f.Airline).LoadAsync(cancellationToken);
        await db.Entry(flight).Reference(f => f.OriginAirport).LoadAsync(cancellationToken);
        await db.Entry(flight).Reference(f => f.DestinationAirport).LoadAsync(cancellationToken);
        return flight;
    }

    public async Task<Flight?> UpdateAsync(Guid id, string? flightNumber, Guid? originAirportId, Guid? destinationAirportId, decimal? basePrice, int? durationMinutes, bool? isActive, CancellationToken cancellationToken = default)
    {
        var flight = await db.Flights.IgnoreQueryFilters()
            .Include(f => f.Airline)
            .Include(f => f.OriginAirport)
            .Include(f => f.DestinationAirport)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (flight is null) return null;

        if (flightNumber is not null) flight.FlightNumber = flightNumber;
        if (originAirportId is not null) flight.OriginAirportId = originAirportId.Value;
        if (destinationAirportId is not null) flight.DestinationAirportId = destinationAirportId.Value;
        if (basePrice is not null) flight.BasePrice = basePrice.Value;
        if (durationMinutes is not null) flight.DurationMinutes = durationMinutes.Value;
        if (isActive is not null) flight.IsActive = isActive.Value;
        flight.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return flight;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var flight = await db.Flights.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (flight is null) return false;

        flight.DeletedAt = DateTime.UtcNow;
        flight.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
