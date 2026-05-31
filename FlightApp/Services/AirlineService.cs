using FlightKS.Data;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class AirlineService(AppDbContext db) : IAirlineService
{
    public async Task<IEnumerable<Airline>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Airlines.AsNoTracking()
            .Include(a => a.LogoFile)
            .Where(a => a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

    public Task<Airline?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Airlines.AsNoTracking()
            .Include(a => a.LogoFile)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IEnumerable<Airline>> GetAllForAdminAsync(CancellationToken cancellationToken = default) =>
        await db.Airlines.IgnoreQueryFilters().AsNoTracking()
            .Include(a => a.LogoFile)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

    public Task<Airline?> GetByIdForAdminAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Airlines.IgnoreQueryFilters().AsNoTracking()
            .Include(a => a.LogoFile)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<Airline> CreateAsync(string code, string name, string country, Guid? logoFileId, CancellationToken cancellationToken = default)
    {
        var codeExists = await db.Airlines.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(a => a.Code == code, cancellationToken);
        if (codeExists) throw new InvalidOperationException($"Airline code '{code}' is already in use.");

        var airline = new Airline { Code = code, Name = name, Country = country, LogoFileId = logoFileId };
        db.Airlines.Add(airline);
        await db.SaveChangesAsync(cancellationToken);

        return await db.Airlines.Include(a => a.LogoFile)
            .FirstAsync(a => a.Id == airline.Id, cancellationToken);
    }

    public async Task<Airline?> UpdateAsync(Guid id, string? code, string? name, string? country, Guid? logoFileId, bool? isActive, CancellationToken cancellationToken = default)
    {
        var airline = await db.Airlines.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (airline is null) return null;

        if (code is not null) airline.Code = code;
        if (name is not null) airline.Name = name;
        if (country is not null) airline.Country = country;
        if (logoFileId is not null) airline.LogoFileId = logoFileId;
        if (isActive is not null) airline.IsActive = isActive.Value;
        airline.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return await db.Airlines.IgnoreQueryFilters().AsNoTracking()
            .Include(a => a.LogoFile)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var airline = await db.Airlines.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (airline is null) return false;

        var now = DateTime.UtcNow;
        airline.IsActive = false;
        airline.DeletedAt = now;
        airline.UpdatedAt = now;

        // Cascade: deactivate all active flights belonging to this airline
        var flights = await db.Flights.IgnoreQueryFilters()
            .Where(f => f.AirlineId == id && f.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var flight in flights)
        {
            flight.IsActive = false;
            flight.DeletedAt = now;
            flight.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<Airline?> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var airline = await db.Airlines.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (airline is null) return null;

        var now = DateTime.UtcNow;
        var deactivatedAt = airline.DeletedAt;

        airline.IsActive = true;
        airline.DeletedAt = null;
        airline.UpdatedAt = now;

        // Cascade: reactivate only the flights that were deactivated when the airline was deactivated
        // (flights independently deactivated before that are left alone)
        var flights = await db.Flights.IgnoreQueryFilters()
            .Where(f => f.AirlineId == id && !f.IsActive && f.DeletedAt == deactivatedAt)
            .ToListAsync(cancellationToken);

        foreach (var flight in flights)
        {
            flight.IsActive = true;
            flight.DeletedAt = null;
            flight.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);

        return await db.Airlines.IgnoreQueryFilters().AsNoTracking()
            .Include(a => a.LogoFile)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }
}
