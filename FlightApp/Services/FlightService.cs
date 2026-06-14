using FlightKS.Data;
using FlightKS.Enums;
using FlightKS.Exceptions;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using NodaTime;

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
        var originTimeZone = await db.Airports.AsNoTracking()
            .Where(a => a.Id == originAirportId)
            .Select(a => a.TimeZone)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "UTC";
        var (dayStart, dayEnd) = GetUtcDateWindow(departureDate, originTimeZone);

        return await db.FlightSchedules.AsNoTracking()
            .Include(s => s.Flight).ThenInclude(f => f.Airline)
            .Include(s => s.Flight).ThenInclude(f => f.OriginAirport)
            .Include(s => s.Flight).ThenInclude(f => f.DestinationAirport)
            .Include(s => s.Aircraft)
            .Where(s =>
                s.Flight.IsActive &&
                s.Flight.OriginAirportId == originAirportId &&
                s.Flight.DestinationAirportId == destinationAirportId &&
                s.DepartureTime >= dayStart &&
                s.DepartureTime < dayEnd &&
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
            .Where(s => s.Flight.IsActive && s.Status == FlightScheduleStatus.Scheduled && s.DepartureTime > DateTime.UtcNow)
            .OrderBy(s => s.CurrentPrice)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Flight> Items, int Total)> GetAllForAdminAsync(
        string? search,
        Guid? airlineId,
        Guid? originAirportId,
        Guid? destinationAirportId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var q = db.Flights.IgnoreQueryFilters().AsNoTracking()
            .Include(f => f.Airline)
            .Include(f => f.OriginAirport)
            .Include(f => f.DestinationAirport)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(f =>
                f.FlightNumber.ToLower().Contains(term) ||
                f.Airline.Name.ToLower().Contains(term) ||
                f.Airline.Code.ToLower().Contains(term) ||
                f.OriginAirport.Code.ToLower().Contains(term) ||
                f.OriginAirport.City.ToLower().Contains(term) ||
                f.DestinationAirport.Code.ToLower().Contains(term) ||
                f.DestinationAirport.City.ToLower().Contains(term));
        }

        if (airlineId is not null)
            q = q.Where(f => f.AirlineId == airlineId);

        if (originAirportId is not null)
            q = q.Where(f => f.OriginAirportId == originAirportId);

        if (destinationAirportId is not null)
            q = q.Where(f => f.DestinationAirportId == destinationAirportId);

        if (isActive is not null)
            q = q.Where(f => f.IsActive == isActive);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderBy(f => f.Airline.Name).ThenBy(f => f.FlightNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<Flight?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Flights.AsNoTracking()
            .Include(f => f.Airline)
            .Include(f => f.OriginAirport)
            .Include(f => f.DestinationAirport)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<Flight> CreateAsync(Guid airlineId, string flightNumber, Guid originAirportId, Guid destinationAirportId, decimal basePrice, CancellationToken cancellationToken = default)
    {
        flightNumber = flightNumber.Trim().ToUpperInvariant();

        await ValidateActiveFlightReferencesAsync(airlineId, originAirportId, destinationAirportId, cancellationToken);

        var dup = await db.Flights.AsNoTracking()
            .AnyAsync(f => f.AirlineId == airlineId && f.FlightNumber.ToLower() == flightNumber.ToLower(), cancellationToken);
        if (dup) throw new ConflictException($"Flight number '{flightNumber}' already exists for this airline.");

        var flight = new Flight
        {
            AirlineId = airlineId,
            FlightNumber = flightNumber,
            OriginAirportId = originAirportId,
            DestinationAirportId = destinationAirportId,
            BasePrice = basePrice,
            DurationMinutes = 0,
        };
        db.Flights.Add(flight);
        await db.SaveChangesAsync(cancellationToken);

        await db.Entry(flight).Reference(f => f.Airline).LoadAsync(cancellationToken);
        await db.Entry(flight).Reference(f => f.OriginAirport).LoadAsync(cancellationToken);
        await db.Entry(flight).Reference(f => f.DestinationAirport).LoadAsync(cancellationToken);
        return flight;
    }

    public async Task<Flight?> UpdateAsync(Guid id, Guid? airlineId, string? flightNumber, Guid? originAirportId, Guid? destinationAirportId, decimal? basePrice, bool? isActive, CancellationToken cancellationToken = default)
    {
        var flight = await db.Flights.IgnoreQueryFilters()
            .Include(f => f.Airline)
            .Include(f => f.OriginAirport)
            .Include(f => f.DestinationAirport)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (flight is null) return null;

        var nextAirlineId = airlineId ?? flight.AirlineId;
        var nextFlightNumber = flightNumber?.Trim().ToUpperInvariant() ?? flight.FlightNumber;
        var nextOriginAirportId = originAirportId ?? flight.OriginAirportId;
        var nextDestinationAirportId = destinationAirportId ?? flight.DestinationAirportId;

        if (nextOriginAirportId == nextDestinationAirportId)
            throw new ValidationException("originAirportId", "Origin and destination airports must differ.");

        if (airlineId is not null || originAirportId is not null || destinationAirportId is not null)
            await ValidateActiveFlightReferencesAsync(nextAirlineId, nextOriginAirportId, nextDestinationAirportId, cancellationToken);

        var duplicate = await db.Flights.AsNoTracking()
            .AnyAsync(f =>
                f.Id != id &&
                f.AirlineId == nextAirlineId &&
                f.FlightNumber.ToLower() == nextFlightNumber.ToLower(),
                cancellationToken);
        if (duplicate)
            throw new ConflictException($"Flight number '{nextFlightNumber}' already exists for this airline.");

        if (airlineId is not null) flight.AirlineId = airlineId.Value;
        if (flightNumber is not null) flight.FlightNumber = nextFlightNumber;
        if (originAirportId is not null) flight.OriginAirportId = originAirportId.Value;
        if (destinationAirportId is not null) flight.DestinationAirportId = destinationAirportId.Value;
        if (basePrice is not null) flight.BasePrice = basePrice.Value;
        if (isActive is not null) flight.IsActive = isActive.Value;
        flight.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(flight).Reference(f => f.Airline).LoadAsync(cancellationToken);
        await db.Entry(flight).Reference(f => f.OriginAirport).LoadAsync(cancellationToken);
        await db.Entry(flight).Reference(f => f.DestinationAirport).LoadAsync(cancellationToken);
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

    private async Task ValidateActiveFlightReferencesAsync(Guid airlineId, Guid originAirportId, Guid destinationAirportId, CancellationToken cancellationToken)
    {
        var airlineExists = await db.Airlines.AsNoTracking()
            .AnyAsync(a => a.Id == airlineId && a.IsActive, cancellationToken);
        if (!airlineExists) throw new NotFoundException($"Active airline '{airlineId}' not found.");

        var activeAirportCount = await db.Airports.AsNoTracking()
            .CountAsync(a => (a.Id == originAirportId || a.Id == destinationAirportId) && a.IsActive, cancellationToken);
        if (activeAirportCount != 2)
            throw new NotFoundException("Origin and destination airports must both be active.");
    }

    private static (DateTime StartUtc, DateTime EndUtc) GetUtcDateWindow(DateOnly date, string timeZone)
    {
        var zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZone) ?? DateTimeZone.Utc;
        var localDate = new LocalDate(date.Year, date.Month, date.Day);
        var start = localDate.AtStartOfDayInZone(zone).ToInstant().ToDateTimeUtc();
        var end = localDate.PlusDays(1).AtStartOfDayInZone(zone).ToInstant().ToDateTimeUtc();
        return (start, end);
    }
}
