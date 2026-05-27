using FlightKS.Data;
using FlightKS.Enums;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class BookingService(AppDbContext db) : IBookingService
{
    public async Task<Booking> CreateAsync(Guid userId, Guid itineraryId, int passengerCount, CancellationToken cancellationToken = default)
    {
        var itinerary = await db.Itineraries.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == itineraryId && i.IsActive, cancellationToken);
        if (itinerary is null)
            throw new InvalidOperationException($"Itinerary '{itineraryId}' not found or inactive.");

        var booking = new Booking
        {
            UserId = userId,
            ItineraryId = itineraryId,
            BookingReference = GenerateReference(),
            Status = BookingStatus.Pending,
            TotalAmount = itinerary.TotalPrice * passengerCount,
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken);
        return booking;
    }

    public async Task<IEnumerable<Booking>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await Load(asNoTracking: true)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<Booking?> GetByIdAsync(Guid bookingId, Guid? ownerUserId = null, CancellationToken cancellationToken = default)
    {
        var q = Load(asNoTracking: true).Where(b => b.Id == bookingId);
        if (ownerUserId is { } uid) q = q.Where(b => b.UserId == uid);
        return await q.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Booking?> GetSummaryAsync(Guid bookingId, Guid? ownerUserId, CancellationToken cancellationToken = default)
    {
        var q = LoadDetailed(asNoTracking: true).Where(b => b.Id == bookingId);
        if (ownerUserId is { } uid) q = q.Where(b => b.UserId == uid);
        return await q.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Booking?> GetConfirmationAsync(Guid bookingId, Guid ownerUserId, CancellationToken cancellationToken = default) =>
        await LoadDetailed(asNoTracking: true)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == ownerUserId, cancellationToken);

    public async Task<Booking?> UpdateStatusAsync(Guid bookingId, BookingStatus status, CancellationToken cancellationToken = default)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking is null) return null;

        booking.Status = status;
        booking.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return booking;
    }

    public async Task<BookingPriceSummary?> GetPriceSummaryAsync(Guid bookingId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var booking = await db.Bookings.AsNoTracking()
            .Include(b => b.Tickets)
            .Include(b => b.BookingBaggage).ThenInclude(bb => bb.BaggageOption)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == ownerUserId, cancellationToken);
        if (booking is null) return null;

        var seatsTotal = booking.Tickets.Sum(t => t.Price);
        var baggageTotal = booking.BookingBaggage.Sum(bb => bb.BaggageOption.Price * bb.Quantity);
        var paidTotal = booking.Payments
            .Where(p => p.PaymentStatus == PaymentStatus.Completed)
            .Sum(p => p.Amount);

        return new BookingPriceSummary(seatsTotal, baggageTotal, paidTotal, seatsTotal + baggageTotal);
    }

    public async Task<bool> CancelAsync(Guid bookingId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == ownerUserId, cancellationToken);
        if (booking is null) return false;

        booking.Status = BookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<Booking> Load(bool asNoTracking)
    {
        var q = db.Bookings
            .Include(b => b.Passengers)
            .Include(b => b.Tickets)
            .AsQueryable();
        return asNoTracking ? q.AsNoTracking() : q;
    }

    private IQueryable<Booking> LoadDetailed(bool asNoTracking)
    {
        var q = db.Bookings
            .Include(b => b.Passengers)
            .Include(b => b.Tickets).ThenInclude(t => t.Passenger)
            .Include(b => b.Tickets).ThenInclude(t => t.FlightSchedule).ThenInclude(s => s.Flight).ThenInclude(f => f.OriginAirport)
            .Include(b => b.Tickets).ThenInclude(t => t.FlightSchedule).ThenInclude(s => s.Flight).ThenInclude(f => f.DestinationAirport)
            .Include(b => b.Tickets).ThenInclude(t => t.FlightSeat).ThenInclude(fs => fs!.Seat)
            .Include(b => b.BookingBaggage).ThenInclude(bb => bb.BaggageOption)
            .Include(b => b.Payments)
            .AsQueryable();
        return asNoTracking ? q.AsNoTracking() : q;
    }

    private static string GenerateReference()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<char> chars = stackalloc char[6];
        var rng = Random.Shared;
        for (var i = 0; i < chars.Length; i++)
            chars[i] = alphabet[rng.Next(alphabet.Length)];
        return $"BKG-{new string(chars)}";
    }
}
