using FlightKS.Data;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class AircraftService(AppDbContext db) : IAircraftService
{
    public async Task<IEnumerable<Aircraft>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Aircrafts.AsNoTracking()
            .Include(a => a.Airline)
            .OrderBy(a => a.Airline.Name).ThenBy(a => a.Model)
            .ToListAsync(cancellationToken);

    public Task<Aircraft?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Aircrafts.AsNoTracking()
            .Include(a => a.Airline)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<Aircraft> CreateAsync(Guid airlineId, string model, string registrationNumber, int totalSeats, CancellationToken cancellationToken = default)
    {
        var airline = await db.Airlines.FirstOrDefaultAsync(a => a.Id == airlineId, cancellationToken)
            ?? throw new InvalidOperationException($"Airline '{airlineId}' not found.");

        var regExists = await db.Aircrafts.AsNoTracking()
            .AnyAsync(a => a.RegistrationNumber == registrationNumber, cancellationToken);
        if (regExists) throw new InvalidOperationException($"Aircraft registration '{registrationNumber}' is already in use.");

        var aircraft = new Aircraft
        {
            AirlineId = airlineId,
            Model = model,
            RegistrationNumber = registrationNumber,
            TotalSeats = totalSeats,
        };
        db.Aircrafts.Add(aircraft);
        await db.SaveChangesAsync(cancellationToken);
        aircraft.Airline = airline;
        return aircraft;
    }

    public async Task<Aircraft?> UpdateAsync(Guid id, Guid? airlineId, string? model, string? registrationNumber, int? totalSeats, bool? isActive, CancellationToken cancellationToken = default)
    {
        var aircraft = await db.Aircrafts.IgnoreQueryFilters()
            .Include(a => a.Airline)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (aircraft is null) return null;

        if (airlineId is not null) aircraft.AirlineId = airlineId.Value;
        if (model is not null) aircraft.Model = model;
        if (registrationNumber is not null) aircraft.RegistrationNumber = registrationNumber;
        if (totalSeats is not null) aircraft.TotalSeats = totalSeats.Value;
        if (isActive is not null) aircraft.IsActive = isActive.Value;
        aircraft.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return aircraft;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var aircraft = await db.Aircrafts.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (aircraft is null) return false;

        aircraft.DeletedAt = DateTime.UtcNow;
        aircraft.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
