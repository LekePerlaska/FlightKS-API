using FlightKS.Data;
using FlightKS.Exceptions;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class PassengerService(AppDbContext db) : IPassengerService
{
    public async Task<IEnumerable<Passenger>> GetForBookingAsync(Guid bookingId, Guid? ownerUserId = null, CancellationToken cancellationToken = default)
    {
        var q = db.Passengers.AsNoTracking().Where(p => p.BookingId == bookingId);
        if (ownerUserId is { } uid) q = q.Where(p => p.Booking.UserId == uid);
        return await q.OrderBy(p => p.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<Passenger> AddAsync(
        Guid bookingId,
        Guid ownerUserId,
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        string? gender = null,
        string? passportNumber = null,
        string? nationality = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureBookingOwnedAsync(bookingId, ownerUserId, cancellationToken);

        var passenger = new Passenger
        {
            BookingId = bookingId,
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = dateOfBirth,
            Gender = gender,
            PassportNumber = passportNumber,
            Nationality = nationality,
        };
        db.Passengers.Add(passenger);
        await db.SaveChangesAsync(cancellationToken);
        return passenger;
    }

    public async Task<Passenger?> UpdateAsync(
        Guid bookingId,
        Guid passengerId,
        Guid ownerUserId,
        string? firstName = null,
        string? lastName = null,
        DateOnly? dateOfBirth = null,
        string? gender = null,
        string? passportNumber = null,
        string? nationality = null,
        CancellationToken cancellationToken = default)
    {
        var passenger = await db.Passengers
            .Where(p => p.Id == passengerId && p.BookingId == bookingId && p.Booking.UserId == ownerUserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (passenger is null) return null;

        if (firstName is not null) passenger.FirstName = firstName;
        if (lastName is not null) passenger.LastName = lastName;
        if (dateOfBirth is not null) passenger.DateOfBirth = dateOfBirth.Value;
        if (gender is not null) passenger.Gender = gender;
        if (passportNumber is not null) passenger.PassportNumber = passportNumber;
        if (nationality is not null) passenger.Nationality = nationality;
        passenger.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return passenger;
    }

    private async Task EnsureBookingOwnedAsync(Guid bookingId, Guid userId, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.AsNoTracking()
            .Select(b => new { b.Id, b.UserId })
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            ?? throw new NotFoundException($"Booking '{bookingId}' not found.");
        if (booking.UserId != userId)
            throw new ForbiddenException("You do not have access to this booking.");
    }
}
