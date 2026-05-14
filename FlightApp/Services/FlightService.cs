using FlightKS.Data;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.Flights;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class FlightService(AppDbContext db) : IFlightService
{
    public async Task<IEnumerable<FlightResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var flights = await db.Flights
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return flights.Select(FlightMapping.ToResponse);
    }

    public async Task<FlightResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var flight = await FindByIdAsync(id, tracked: false, cancellationToken);
        return flight?.ToResponse();
    }

    public async Task<FlightResponseDto> CreateAsync(FlightCreateDto dto, CancellationToken cancellationToken = default)
    {
        var flight = dto.ToEntity();

        db.Flights.Add(flight);
        await db.SaveChangesAsync(cancellationToken);

        return flight.ToResponse();
    }

    public async Task<FlightResponseDto?> UpdateAsync(Guid id, FlightUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var flight = await FindByIdAsync(id, tracked: true, cancellationToken);
        if (flight is null) return null;

        dto.ApplyTo(flight);
        await db.SaveChangesAsync(cancellationToken);

        return flight.ToResponse();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var flight = await FindByIdAsync(id, tracked: true, cancellationToken);
        if (flight is null) return false;

        db.Flights.Remove(flight);
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    private Task<Flight?> FindByIdAsync(Guid id, bool tracked, CancellationToken cancellationToken)
    {
        var query = tracked ? db.Flights : db.Flights.AsNoTracking();
        return query.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }
}
