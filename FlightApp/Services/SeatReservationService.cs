using FlightKS.Data;
using FlightKS.Enums;
using FlightKS.Exceptions;
using FlightKS.Hubs;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class SeatReservationService(AppDbContext db, IHubContext<SeatHub> seatHub) : ISeatReservationService
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
        Guid seatId,
        Guid itinerarySegmentId,
        TimeSpan? holdFor = null,
        CancellationToken cancellationToken = default)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            ?? throw new NotFoundException($"Booking '{bookingId}' not found.");
        if (booking.UserId != ownerUserId)
            throw new ForbiddenException("You do not have access to this booking.");

        _ = await db.Passengers.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == passengerId && p.BookingId == bookingId, cancellationToken)
            ?? throw new NotFoundException($"Passenger '{passengerId}' not found in booking '{bookingId}'.");

        var segment = await db.ItinerarySegments.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == itinerarySegmentId, cancellationToken)
            ?? throw new NotFoundException($"Itinerary segment '{itinerarySegmentId}' not found.");

        var schedule = await db.FlightSchedules.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == segment.FlightScheduleId, cancellationToken)
            ?? throw new NotFoundException($"Flight schedule '{segment.FlightScheduleId}' not found.");

        var flightSeat = await db.FlightSeats
            .Include(fs => fs.Seat)
            .FirstOrDefaultAsync(fs => fs.SeatId == seatId && fs.FlightScheduleId == segment.FlightScheduleId, cancellationToken);

        if (flightSeat is null)
        {
            var aircraftSeat = await db.Seats.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == seatId && s.AircraftId == schedule.AircraftId, cancellationToken)
                ?? throw new NotFoundException($"Seat '{seatId}' not found on this flight's aircraft.");

            var classPrice = await db.FlightSchedulePrices.AsNoTracking()
                .Where(p => p.FlightScheduleId == segment.FlightScheduleId && p.SeatClass == aircraftSeat.SeatClass)
                .Select(p => (decimal?)p.Price)
                .FirstOrDefaultAsync(cancellationToken);

            flightSeat = new FlightSeat
            {
                SeatId = seatId,
                FlightScheduleId = segment.FlightScheduleId,
                Status = FlightSeatStatus.Available,
                Price = classPrice ?? schedule.CurrentPrice,
            };
            db.FlightSeats.Add(flightSeat);
            await db.SaveChangesAsync(cancellationToken);
            flightSeat.Seat = aircraftSeat;
        }

        if (flightSeat.Status != FlightSeatStatus.Available)
            throw new ConflictException("This seat is no longer available.");

        flightSeat.Status = FlightSeatStatus.Reserved;
        flightSeat.ReservedUntil = DateTime.UtcNow.Add(holdFor ?? DefaultHold);
        flightSeat.UpdatedAt = DateTime.UtcNow;

        var ticket = new Ticket
        {
            BookingId = bookingId,
            PassengerId = passengerId,
            FlightScheduleId = segment.FlightScheduleId,
            FlightSeatId = flightSeat.Id,
            TicketNumber = GenerateTicketNumber(),
            TicketStatus = TicketStatus.Issued,
            Price = flightSeat.Price,
            IssuedAt = DateTime.UtcNow,
        };
        db.Tickets.Add(ticket);

        await db.SaveChangesAsync(cancellationToken);
        await seatHub.Clients.Group(segment.FlightScheduleId.ToString())
            .SendAsync("SeatReserved", flightSeat.Id, cancellationToken: cancellationToken);
        return new SeatReservationResult(flightSeat, ticket);
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
        await seatHub.Clients.Group(seat.FlightScheduleId.ToString())
            .SendAsync("SeatReleased", seat.Id, cancellationToken: cancellationToken);
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
