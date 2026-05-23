using FlightKS.Data;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class AirlineService(AppDbContext db) : IAirlineService
{
    public async Task<IEnumerable<Airline>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Airlines.AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

    public Task<Airline?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Airlines.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IEnumerable<Airline>> GetAllForAdminAsync(CancellationToken cancellationToken = default) =>
        await db.Airlines.IgnoreQueryFilters().AsNoTracking()
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

    public async Task<Airline> CreateAsync(string code, string name, string country, Guid? logoFileId, CancellationToken cancellationToken = default)
    {
        var codeExists = await db.Airlines.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(a => a.Code == code, cancellationToken);
        if (codeExists) throw new InvalidOperationException($"Airline code '{code}' is already in use.");

        var airline = new Airline { Code = code, Name = name, Country = country, LogoFileId = logoFileId };
        db.Airlines.Add(airline);
        await db.SaveChangesAsync(cancellationToken);
        return airline;
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
        return airline;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var airline = await db.Airlines.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (airline is null) return false;

        airline.DeletedAt = DateTime.UtcNow;
        airline.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
