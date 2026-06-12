using FlightKS.Data;
using FlightKS.Exceptions;
using FlightKS.Models.Dtos.Aircrafts;
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

    public async Task<IEnumerable<Aircraft>> GetAllForAdminAsync(CancellationToken cancellationToken = default) =>
        await db.Aircrafts.IgnoreQueryFilters().AsNoTracking()
            .Include(a => a.Airline)
            .OrderBy(a => a.Airline.Name).ThenBy(a => a.Model)
            .ToListAsync(cancellationToken);

    public Task<Aircraft?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Aircrafts.AsNoTracking()
            .Include(a => a.Airline)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<Aircraft> CreateAsync(Guid airlineId, string model, string registrationNumber, int totalSeats, CancellationToken cancellationToken = default)
    {
        registrationNumber = registrationNumber.Trim().ToUpperInvariant();

        var airline = await db.Airlines.FirstOrDefaultAsync(a => a.Id == airlineId && a.IsActive, cancellationToken)
            ?? throw new NotFoundException($"Active airline '{airlineId}' not found.");

        var regExists = await db.Aircrafts.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(a => a.RegistrationNumber.ToLower() == registrationNumber.ToLower(), cancellationToken);
        if (regExists) throw new ConflictException($"Aircraft registration '{registrationNumber}' is already in use.");

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

        registrationNumber = registrationNumber?.Trim().ToUpperInvariant();

        if (airlineId is not null)
        {
            var airlineExists = await db.Airlines.AsNoTracking()
                .AnyAsync(a => a.Id == airlineId && a.IsActive, cancellationToken);
            if (!airlineExists) throw new NotFoundException($"Active airline '{airlineId}' not found.");
        }

        if (registrationNumber is not null)
        {
            var regExists = await db.Aircrafts.IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(a => a.Id != id && a.RegistrationNumber.ToLower() == registrationNumber.ToLower(), cancellationToken);
            if (regExists) throw new ConflictException($"Aircraft registration '{registrationNumber}' is already in use.");
        }

        if (airlineId is not null) aircraft.AirlineId = airlineId.Value;
        if (model is not null) aircraft.Model = model;
        if (registrationNumber is not null) aircraft.RegistrationNumber = registrationNumber;
        if (totalSeats is not null) aircraft.TotalSeats = totalSeats.Value;
        if (isActive is not null) aircraft.IsActive = isActive.Value;
        aircraft.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        if (airlineId is not null)
        {
            await db.Entry(aircraft).Reference(a => a.Airline).LoadAsync(cancellationToken);
        }
        return aircraft;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var aircraft = await db.Aircrafts.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (aircraft is null) return false;

        var isAssignedToSchedule = await db.FlightSchedules.AsNoTracking()
            .AnyAsync(s => s.AircraftId == id, cancellationToken);
        if (isAssignedToSchedule)
            throw new BusinessRuleException("Cannot delete an aircraft that is assigned to flight schedules.");

        aircraft.DeletedAt = DateTime.UtcNow;
        aircraft.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<Seat>> GetSeatsAsync(Guid aircraftId, CancellationToken cancellationToken = default) =>
        await db.Seats.AsNoTracking()
            .Where(s => s.AircraftId == aircraftId)
            .OrderBy(s => s.SeatNumber)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Seat>> GenerateSeatsAsync(
        Guid aircraftId,
        IReadOnlyList<SeatCreateItemDto> seatDtos,
        CancellationToken cancellationToken = default)
    {
        var aircraft = await db.Aircrafts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == aircraftId, cancellationToken)
            ?? throw new NotFoundException($"Aircraft '{aircraftId}' not found.");

        var requestedSeatNumbers = seatDtos.Select(s => s.SeatNumber).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (requestedSeatNumbers.Count != seatDtos.Count)
            throw new ValidationException("seatNumbers", "Seat numbers must be unique within the generated layout.");

        var existing = await db.Seats.IgnoreQueryFilters()
            .Where(s => s.AircraftId == aircraftId)
            .ToListAsync(cancellationToken);
        var existingBySeatNumber = existing.ToDictionary(s => s.SeatNumber, StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        foreach (var seat in existing.Where(s => !requestedSeatNumbers.Contains(s.SeatNumber)))
        {
            seat.DeletedAt = now;
            seat.UpdatedAt = now;
        }

        var generatedSeats = new List<Seat>(seatDtos.Count);
        foreach (var seatDto in seatDtos)
        {
            if (existingBySeatNumber.TryGetValue(seatDto.SeatNumber, out var existingSeat))
            {
                existingSeat.SeatClass = seatDto.SeatClass;
                existingSeat.IsWindow = seatDto.IsWindow;
                existingSeat.IsAisle = seatDto.IsAisle;
                existingSeat.ExtraLegroom = seatDto.ExtraLegroom;
                existingSeat.DeletedAt = null;
                existingSeat.UpdatedAt = now;
                generatedSeats.Add(existingSeat);
                continue;
            }

            var newSeat = new Seat
            {
                AircraftId = aircraftId,
                SeatNumber = seatDto.SeatNumber,
                SeatClass = seatDto.SeatClass,
                IsWindow = seatDto.IsWindow,
                IsAisle = seatDto.IsAisle,
                ExtraLegroom = seatDto.ExtraLegroom,
            };
            db.Seats.Add(newSeat);
            generatedSeats.Add(newSeat);
        }

        aircraft.TotalSeats = generatedSeats.Count;
        aircraft.UpdatedAt = now;

        var schedules = await db.FlightSchedules
            .Where(s => s.AircraftId == aircraftId && s.DeletedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var schedule in schedules)
        {
            schedule.AvailableSeats = generatedSeats.Count;
            schedule.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return generatedSeats;
    }
}
