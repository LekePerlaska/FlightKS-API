using FlightKS.Data;
using FlightKS.Enums;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class SeatReservationService(AppDbContext db) : ISeatReservationService
{
    private static readonly TimeSpan DefaultHold = TimeSpan.FromMinutes(15);

    public async Task<IEnumerable<FlightSeat>> GetForBookingAsync(Guid bookingId, Guid? ownerUserId = null, CancellationToken cancellationToken = default)
    {
        var q = db.Tickets.AsNoTracking()
            .Where(t => t.BookingId == bookingId && t.FlightSeatId != null);
        if (ownerUserId is { } uid) q = q.Where(t => t.Booking.UserId == uid);
        return await q
            .Select(t => t.FlightSeat!)
            .Include(fs => fs.Seat)
            .ToListAsync(cancellationToken);
    }

    public async Task<SeatReservationResult> ReserveAsync(
        Guid bookingId,
        Guid ownerUserId,
        Guid passengerId,
        Guid flightSeatId,
        TimeSpan? holdFor = null,
        CancellationToken cancellationToken = default)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == ownerUserId, cancellationToken)
            ?? throw new InvalidOperationException($"Booking '{bookingId}' not found for this user.");

        var passenger = await db.Passengers.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == passengerId && p.BookingId == bookingId, cancellationToken)
            ?? throw new InvalidOperationException($"Passenger '{passengerId}' not part of booking '{bookingId}'.");

        var seat = await db.FlightSeats
            .Include(fs => fs.Seat)
            .FirstOrDefaultAsync(fs => fs.Id == flightSeatId, cancellationToken)
            ?? throw new InvalidOperationException($"Flight seat '{flightSeatId}' not found.");

        if (seat.Status != FlightSeatStatus.Available)
            throw new InvalidOperationException($"Flight seat '{flightSeatId}' is not available.");

        seat.Status = FlightSeatStatus.Reserved;
        seat.ReservedUntil = DateTime.UtcNow.Add(holdFor ?? DefaultHold);
        seat.UpdatedAt = DateTime.UtcNow;

        var ticket = new Ticket
        {
            BookingId = bookingId,
            PassengerId = passengerId,
            FlightScheduleId = seat.FlightScheduleId,
            FlightSeatId = seat.Id,
            TicketNumber = GenerateTicketNumber(),
            TicketStatus = TicketStatus.Issued,
            Price = seat.Price,
            IssuedAt = DateTime.UtcNow,
        };
        db.Tickets.Add(ticket);

        await db.SaveChangesAsync(cancellationToken);
        return new SeatReservationResult(seat, ticket);
    }

    public async Task<bool> ReleaseAsync(Guid bookingId, Guid ownerUserId, Guid flightSeatId, CancellationToken cancellationToken = default)
    {
        var owned = await db.Bookings.AsNoTracking()
            .AnyAsync(b => b.Id == bookingId && b.UserId == ownerUserId, cancellationToken);
        if (!owned) return false;

        var seat = await db.FlightSeats.FirstOrDefaultAsync(fs => fs.Id == flightSeatId, cancellationToken);
        if (seat is null || seat.Status != FlightSeatStatus.Reserved) return false;

        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.BookingId == bookingId && t.FlightSeatId == flightSeatId, cancellationToken);
        if (ticket is not null) db.Tickets.Remove(ticket);

        seat.Status = FlightSeatStatus.Available;
        seat.ReservedUntil = null;
        seat.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string GenerateTicketNumber()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<char> chars = stackalloc char[8];
        var rng = Random.Shared;
        for (var i = 0; i < chars.Length; i++)
            chars[i] = alphabet[rng.Next(alphabet.Length)];
        return $"TKT-{new string(chars)}";
    }
}
