using FlightKS.Data;
using FlightKS.Exceptions;
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
        await EnsureBookingOwnedAsync(bookingId, ownerUserId, cancellationToken);

        var passengerOk = await db.Passengers.AsNoTracking()
            .AnyAsync(p => p.Id == passengerId && p.BookingId == bookingId, cancellationToken);
        if (!passengerOk) throw new NotFoundException($"Passenger '{passengerId}' not found in booking '{bookingId}'.");

        var optionExists = await db.BaggageOptions.AsNoTracking()
            .AnyAsync(bo => bo.Id == baggageOptionId && bo.IsActive, cancellationToken);
        if (!optionExists) throw new NotFoundException($"Baggage option '{baggageOptionId}' not found or is inactive.");

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
