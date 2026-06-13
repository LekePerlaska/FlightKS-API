using FlightKS.Data;
using FlightKS.Enums;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class TicketService(AppDbContext db, INotificationService notificationService) : ITicketService
{
    public async Task<Ticket?> GetByIdAsync(Guid ticketId, Guid? ownerUserId = null, CancellationToken cancellationToken = default)
    {
        var q = db.Tickets.AsNoTracking()
            .Include(t => t.Passenger)
            .Include(t => t.FlightSchedule).ThenInclude(s => s.Flight).ThenInclude(f => f.Airline)
            .Include(t => t.FlightSchedule).ThenInclude(s => s.Flight).ThenInclude(f => f.OriginAirport)
            .Include(t => t.FlightSchedule).ThenInclude(s => s.Flight).ThenInclude(f => f.DestinationAirport)
            .Include(t => t.FlightSeat).ThenInclude(fs => fs!.Seat)
            .Where(t => t.Id == ticketId);
        if (ownerUserId is { } uid) q = q.Where(t => t.Booking.UserId == uid);
        return await q.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Ticket>> GetForBookingAsync(Guid bookingId, Guid? ownerUserId = null, CancellationToken cancellationToken = default)
    {
        var q = db.Tickets.AsNoTracking()
            .Include(t => t.Passenger)
            .Include(t => t.FlightSchedule).ThenInclude(s => s.Flight).ThenInclude(f => f.OriginAirport)
            .Include(t => t.FlightSchedule).ThenInclude(s => s.Flight).ThenInclude(f => f.DestinationAirport)
            .Include(t => t.FlightSeat).ThenInclude(fs => fs!.Seat)
            .Where(t => t.BookingId == bookingId);
        if (ownerUserId is { } uid) q = q.Where(t => t.Booking.UserId == uid);
        return await q.OrderBy(t => t.IssuedAt).ToListAsync(cancellationToken);
    }

    public async Task<Ticket?> UpdateStatusAsync(Guid ticketId, TicketStatus status, CancellationToken cancellationToken = default)
    {
        var ticket = await db.Tickets
            .Include(t => t.Booking)
            .Include(t => t.FlightSchedule).ThenInclude(s => s.Flight).ThenInclude(f => f.OriginAirport)
            .Include(t => t.FlightSchedule).ThenInclude(s => s.Flight).ThenInclude(f => f.DestinationAirport)
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);
        if (ticket is null) return null;

        var oldStatus = ticket.TicketStatus;
        ticket.TicketStatus = status;
        ticket.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        if (status != oldStatus && status is TicketStatus.Cancelled or TicketStatus.Refunded)
        {
            var flightNumber = ticket.FlightSchedule.Flight.FlightNumber;
            var origin = ticket.FlightSchedule.Flight.OriginAirport.Code;
            var destination = ticket.FlightSchedule.Flight.DestinationAirport.Code;

            await notificationService.CreateAsync(ticket.Booking.UserId,
                "Ticket Cancelled",
                $"Your ticket for flight {flightNumber} ({origin} → {destination}) has been cancelled.",
                "ticket_cancelled",
                relatedEntityName: "Ticket", relatedEntityId: ticket.Id,
                cancellationToken: cancellationToken);
        }

        return ticket;
    }
}
