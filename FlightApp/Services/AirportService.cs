using FlightKS.Data;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class AirportService(AppDbContext db) : IAirportService
{
    public async Task<IEnumerable<Airport>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Airports.AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.City)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Airport>> AutocompleteAsync(string query, int limit = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        var like = $"%{query.Trim()}%";
        return await db.Airports.AsNoTracking()
            .Where(a => a.IsActive && (
                EF.Functions.ILike(a.Code, like) ||
                EF.Functions.ILike(a.City, like) ||
                EF.Functions.ILike(a.Name, like) ||
                EF.Functions.ILike(a.Country, like)))
            .OrderBy(a => a.City)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task<Airport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Airports.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IEnumerable<Airport>> GetAllForAdminAsync(CancellationToken cancellationToken = default) =>
        await db.Airports.IgnoreQueryFilters().AsNoTracking()
            .OrderBy(a => a.City)
            .ToListAsync(cancellationToken);

    public async Task<Airport> CreateAsync(string code, string name, string city, string country, string timeZone, CancellationToken cancellationToken = default)
    {
        code = code.Trim().ToUpperInvariant();
        name = name.Trim();

        var codeExists = await db.Airports.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(a => a.Code.ToLower() == code.ToLower(), cancellationToken);
        if (codeExists) throw new InvalidOperationException($"Airport code '{code}' is already in use.");

        var nameExists = await db.Airports.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(a => a.Name.ToLower() == name.ToLower(), cancellationToken);
        if (nameExists) throw new InvalidOperationException($"Airport name '{name}' is already in use.");

        var airport = new Airport { Code = code, Name = name, City = city, Country = country, TimeZone = timeZone };
        db.Airports.Add(airport);
        await db.SaveChangesAsync(cancellationToken);
        return airport;
    }

    public async Task<Airport?> UpdateAsync(Guid id, string? code, string? name, string? city, string? country, string? timeZone, bool? isActive, CancellationToken cancellationToken = default)
    {
        var airport = await db.Airports.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (airport is null) return null;

        code = code?.Trim().ToUpperInvariant();
        name = name?.Trim();

        if (code is not null)
        {
            var codeExists = await db.Airports.IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(a => a.Id != id && a.Code.ToLower() == code.ToLower(), cancellationToken);
            if (codeExists) throw new InvalidOperationException($"Airport code '{code}' is already in use.");
        }

        if (name is not null)
        {
            var nameExists = await db.Airports.IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(a => a.Id != id && a.Name.ToLower() == name.ToLower(), cancellationToken);
            if (nameExists) throw new InvalidOperationException($"Airport name '{name}' is already in use.");
        }

        if (code is not null) airport.Code = code;
        if (name is not null) airport.Name = name;
        if (city is not null) airport.City = city;
        if (country is not null) airport.Country = country;
        if (timeZone is not null) airport.TimeZone = timeZone;
        if (isActive is not null) airport.IsActive = isActive.Value;
        airport.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return airport;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var airport = await db.Airports.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (airport is null) return false;

        var isUsedByFlight = await db.Flights.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(f => f.OriginAirportId == id || f.DestinationAirportId == id, cancellationToken);
        var isUsedByItinerary = await db.Itineraries.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(i => i.OriginAirportId == id || i.DestinationAirportId == id, cancellationToken);
        if (isUsedByFlight || isUsedByItinerary)
            throw new InvalidOperationException("Cannot delete an airport that is used by flights or itineraries. Deactivate it instead.");

        db.Airports.Remove(airport);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
