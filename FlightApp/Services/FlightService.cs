using FlightKS.Data;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.Flights;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class FlightService(AppDbContext db) : IFlightService
{
    public async Task<IEnumerable<FlightResponseDto>> SearchAsync(FlightSearchQuery query, CancellationToken cancellationToken = default)
    {
        var departStart = query.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var departEnd = query.Date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var flights = await db.Flights
            .AsNoTracking()
            .Include(f => f.OriginAirport)
            .Include(f => f.DestinationAirport)
            .Include(f => f.Prices)
            .Where(f =>
                f.Origin == query.Origin &&
                f.Destination == query.Destination &&
                f.DepartureTime >= departStart &&
                f.DepartureTime <= departEnd &&
                f.Prices.Any(p => p.Cabin == query.Cabin && p.TotalSeats >= query.Adults))
            .OrderBy(f => f.DepartureTime)
            .ToListAsync(cancellationToken);

        return flights.Select(FlightMapping.ToResponse);
    }

    public async Task<FlightResponseDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var flight = await db.Flights
            .AsNoTracking()
            .Include(f => f.OriginAirport)
            .Include(f => f.DestinationAirport)
            .Include(f => f.Prices)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        return flight?.ToResponse();
    }
}
