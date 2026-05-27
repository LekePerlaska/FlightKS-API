using FlightKS.Data;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class BookingBaggageService(AppDbContext db) : IBookingBaggageService
{
    public async Task<IEnumerable<BookingBaggage>> GetForBookingAsync(Guid bookingId, Guid? ownerUserId = null, CancellationToken cancellationToken = default)
    {
        var q = db.BookingBaggage.AsNoTracking()
            .Include(bb => bb.BaggageOption)
            .Where(bb => bb.BookingId == bookingId);
        if (ownerUserId is { } uid) q = q.Where(bb => bb.Booking.UserId == uid);
        return await q.OrderBy(bb => bb.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<BookingBaggage> AddAsync(
        Guid bookingId,
        Guid ownerUserId,
        Guid passengerId,
        Guid baggageOptionId,
        int quantity = 1,
        CancellationToken cancellationToken = default)
    {
        var owned = await db.Bookings.AsNoTracking()
            .AnyAsync(b => b.Id == bookingId && b.UserId == ownerUserId, cancellationToken);
        if (!owned) throw new InvalidOperationException($"Booking '{bookingId}' not found for this user.");

        var passengerOk = await db.Passengers.AsNoTracking()
            .AnyAsync(p => p.Id == passengerId && p.BookingId == bookingId, cancellationToken);
        if (!passengerOk) throw new InvalidOperationException($"Passenger '{passengerId}' not part of booking '{bookingId}'.");

        var optionExists = await db.BaggageOptions.AsNoTracking()
            .AnyAsync(bo => bo.Id == baggageOptionId && bo.IsActive, cancellationToken);
        if (!optionExists) throw new InvalidOperationException($"Baggage option '{baggageOptionId}' not available.");

        var entity = new BookingBaggage
        {
            BookingId = bookingId,
            PassengerId = passengerId,
            BaggageOptionId = baggageOptionId,
            Quantity = quantity,
        };
        db.BookingBaggage.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return await db.BookingBaggage
            .Include(bb => bb.BaggageOption)
            .FirstAsync(bb => bb.Id == entity.Id, cancellationToken);
    }

    public async Task<BookingBaggage?> UpdateQuantityAsync(
        Guid bookingId,
        Guid bookingBaggageId,
        Guid ownerUserId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var item = await db.BookingBaggage
            .Include(bb => bb.BaggageOption)
            .Where(bb => bb.Id == bookingBaggageId && bb.BookingId == bookingId && bb.Booking.UserId == ownerUserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (item is null) return null;

        item.Quantity = quantity;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<bool> RemoveAsync(Guid bookingId, Guid bookingBaggageId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var item = await db.BookingBaggage
            .Where(bb => bb.Id == bookingBaggageId && bb.BookingId == bookingId && bb.Booking.UserId == ownerUserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (item is null) return false;

        db.BookingBaggage.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
