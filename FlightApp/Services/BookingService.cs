using FlightKS.Data;
using FlightKS.Enums;
using FlightKS.Exceptions;
using FlightKS.Hubs;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class BookingService(AppDbContext db, INotificationService notificationService, IHubContext<SeatHub> seatHub) : IBookingService
{
    public async Task<Booking> CreateAsync(Guid userId, Guid itineraryId, int passengerCount, SeatClass? cabinClass = null, CancellationToken cancellationToken = default)
    {
        var itinerary = await db.Itineraries.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == itineraryId && i.IsActive, cancellationToken)
            ?? throw new NotFoundException($"Itinerary '{itineraryId}' not found or is no longer active.");

        var perPassenger = itinerary.TotalPrice;
        if (cabinClass is { } cls)
        {
            var segmentPrices = await db.ItinerarySegments.AsNoTracking()
                .Where(s => s.ItineraryId == itineraryId)
                .Select(s => new
                {
                    ClassPrice = db.FlightSchedulePrices
                        .Where(p => p.FlightScheduleId == s.FlightScheduleId && p.SeatClass == cls)
                        .Select(p => (decimal?)p.Price)
                        .FirstOrDefault(),
                    Fallback = s.FlightSchedule.CurrentPrice,
                })
                .ToListAsync(cancellationToken);
            if (segmentPrices.Count > 0)
                perPassenger = segmentPrices.Sum(x => x.ClassPrice ?? x.Fallback);
        }

        var booking = new Booking
        {
            UserId = userId,
            ItineraryId = itineraryId,
            BookingReference = GenerateReference(),
            Status = BookingStatus.Pending,
            CabinClass = cabinClass,
            TotalAmount = perPassenger * passengerCount,
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken);

        await ReleaseAbandonedReservationsAsync(userId, booking.Id, cancellationToken);

        return booking;
    }

    private async Task ReleaseAbandonedReservationsAsync(Guid userId, Guid exceptBookingId, CancellationToken cancellationToken)
    {
        var stalePendingIds = await db.Bookings
            .Where(b => b.UserId == userId && b.Status == BookingStatus.Pending && b.Id != exceptBookingId)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);
        if (stalePendingIds.Count == 0) return;

        var heldTickets = await db.Tickets
            .Include(t => t.FlightSeat)
            .Where(t => stalePendingIds.Contains(t.BookingId)
                && t.FlightSeat != null
                && t.FlightSeat.Status == FlightSeatStatus.Reserved)
            .ToListAsync(cancellationToken);

        var released = new List<(Guid ScheduleId, Guid FlightSeatId)>();
        foreach (var ticket in heldTickets)
        {
            ticket.FlightSeat!.Status = FlightSeatStatus.Available;
            ticket.FlightSeat.ReservedUntil = null;
            ticket.FlightSeat.UpdatedAt = DateTime.UtcNow;
            released.Add((ticket.FlightSeat.FlightScheduleId, ticket.FlightSeat.Id));
        }
        db.Tickets.RemoveRange(heldTickets);

        var staleBookings = await db.Bookings
            .Where(b => stalePendingIds.Contains(b.Id))
            .ToListAsync(cancellationToken);
        foreach (var stale in staleBookings)
        {
            stale.Status = BookingStatus.Expired;
            stale.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var (scheduleId, flightSeatId) in released)
            await seatHub.Clients.Group(scheduleId.ToString())
                .SendAsync("SeatReleased", flightSeatId, cancellationToken: cancellationToken);
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
        var booking = await db.Bookings
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking is null) return null;

        var oldStatus = booking.Status;
        booking.Status = status;
        booking.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        if (status != oldStatus)
        {
            var (title, message, type, emailSubject, emailHtml) = status switch
            {
                BookingStatus.Cancelled => (
                    "Booking Cancelled",
                    $"Your booking {booking.BookingReference} has been cancelled.",
                    "booking_cancelled",
                    $"Booking Cancelled – {booking.BookingReference}",
                    EmailTemplates.BookingCancelled(booking.User?.FullName ?? booking.UserId.ToString(), booking.BookingReference)),
                BookingStatus.Confirmed => (
                    "Booking Confirmed",
                    $"Your booking {booking.BookingReference} has been confirmed.",
                    "booking_confirmed",
                    (string?)null,
                    (string?)null),
                _ => (
                    "Booking Updated",
                    $"Your booking {booking.BookingReference} status has been updated to {status}.",
                    "booking_updated",
                    (string?)null,
                    (string?)null)
            };

            await notificationService.CreateAsync(booking.UserId,
                title, message, type,
                relatedEntityName: "Booking", relatedEntityId: booking.Id,
                sendEmail: emailSubject is not null,
                emailSubject: emailSubject,
                emailHtml: emailHtml,
                cancellationToken: cancellationToken);
        }

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
        var baggageTotal = booking.BookingBaggage.Sum(bb => bb.BaggageOption is not null ? bb.BaggageOption.Price * bb.Quantity : 0m);
        var paidTotal = booking.Payments
            .Where(p => p.PaymentStatus == PaymentStatus.Completed)
            .Sum(p => p.Amount);

        return new BookingPriceSummary(seatsTotal, baggageTotal, paidTotal, seatsTotal + baggageTotal);
    }

    public async Task<bool> CancelAsync(Guid bookingId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var booking = await db.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == ownerUserId, cancellationToken);
        if (booking is null) return false;

        booking.Status = BookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await notificationService.CreateAsync(
            booking.UserId,
            "Booking Cancelled",
            $"Your booking {booking.BookingReference} has been cancelled.",
            "booking_cancelled",
            relatedEntityName: "Booking", relatedEntityId: booking.Id,
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task<(IReadOnlyList<Booking> Items, int Total)> GetAllForAdminAsync(
        string? search,
        BookingStatus? status,
        PaymentStatus? paymentStatus,
        DateOnly? createdDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var q = LoadAdmin(asNoTracking: true);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(b =>
                b.BookingReference.ToLower().Contains(term) ||
                b.User.FullName.ToLower().Contains(term) ||
                b.User.Email.ToLower().Contains(term));
        }

        if (status is not null)
            q = q.Where(b => b.Status == status);

        if (paymentStatus is not null)
            q = q.Where(b =>
                b.Payments
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => (PaymentStatus?)p.PaymentStatus)
                    .FirstOrDefault() == paymentStatus);

        if (createdDate is not null)
        {
            var start = DateTime.SpecifyKind(createdDate.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var end = start.AddDays(1);
            q = q.Where(b => b.CreatedAt >= start && b.CreatedAt < end);
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<Booking?> GetDetailForAdminAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        LoadDetailed(asNoTracking: true)
            .Include(b => b.Payments)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

    private IQueryable<Booking> Load(bool asNoTracking)
    {
        var q = db.Bookings
            .Include(b => b.Passengers)
            .Include(b => b.Tickets)
            .Include(b => b.Payments)
            .Include(b => b.Itinerary).ThenInclude(i => i!.OriginAirport)
            .Include(b => b.Itinerary).ThenInclude(i => i!.DestinationAirport)
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

    private IQueryable<Booking> LoadAdmin(bool asNoTracking)
    {
        var q = db.Bookings
            .Include(b => b.User)
            .Include(b => b.Passengers)
            .Include(b => b.Tickets)
            .Include(b => b.Payments)
            .Include(b => b.Itinerary).ThenInclude(i => i!.OriginAirport)
            .Include(b => b.Itinerary).ThenInclude(i => i!.DestinationAirport)
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
