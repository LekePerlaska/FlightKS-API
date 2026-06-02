using FlightKS.Data;
using FlightKS.Enums;
using FlightKS.Exceptions;
using FlightKS.Hubs;
using FlightKS.Models.Dtos.FlightManager;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class FlightManagerService(AppDbContext db, IHubContext<SeatHub> seatHub)
    : IFlightManagerService
{
    public async Task<IEnumerable<FlightManagerSeatDto>> GetSeatsAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await db.FlightSchedules.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);
        if (schedule is null) return [];

        var seats = await db.Seats.AsNoTracking()
            .Where(s => s.AircraftId == schedule.AircraftId)
            .OrderBy(s => s.SeatNumber)
            .ToListAsync(cancellationToken);

        var flightSeats = await db.FlightSeats.AsNoTracking()
            .Where(fs => fs.FlightScheduleId == scheduleId)
            .ToListAsync(cancellationToken);
        var bySeatId = flightSeats.ToDictionary(fs => fs.SeatId);

        var priceByClass = await db.FlightSchedulePrices.AsNoTracking()
            .Where(p => p.FlightScheduleId == scheduleId)
            .ToDictionaryAsync(p => p.SeatClass, p => p.Price, cancellationToken);

        return seats.Select(s =>
        {
            bySeatId.TryGetValue(s.Id, out var fs);
            var price = fs?.Price
                ?? (priceByClass.TryGetValue(s.SeatClass, out var p) ? p : schedule.CurrentPrice);
            return new FlightManagerSeatDto(
                s.Id,
                fs?.Id,
                s.SeatNumber,
                s.SeatClass,
                fs?.Status ?? FlightSeatStatus.Available,
                price);
        });
    }

    public async Task<FlightManagerSeatDto?> SetSeatStatusAsync(Guid scheduleId, Guid seatId, FlightSeatStatus status, CancellationToken cancellationToken = default)
    {
        if (status is not (FlightSeatStatus.Available or FlightSeatStatus.Blocked))
            throw new ValidationException("status", "Seats can only be blocked or released.");

        var schedule = await db.FlightSchedules.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);
        if (schedule is null) return null;

        var seat = await db.Seats.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == seatId && s.AircraftId == schedule.AircraftId, cancellationToken)
            ?? throw new NotFoundException("Seat not found on this flight's aircraft.");

        var flightSeat = await db.FlightSeats
            .FirstOrDefaultAsync(fs => fs.FlightScheduleId == scheduleId && fs.SeatId == seatId, cancellationToken);

        var price = await db.FlightSchedulePrices.AsNoTracking()
            .Where(p => p.FlightScheduleId == scheduleId && p.SeatClass == seat.SeatClass)
            .Select(p => (decimal?)p.Price)
            .FirstOrDefaultAsync(cancellationToken) ?? schedule.CurrentPrice;

        if (flightSeat is null)
        {
            if (status == FlightSeatStatus.Available)
                return new FlightManagerSeatDto(seat.Id, null, seat.SeatNumber, seat.SeatClass, FlightSeatStatus.Available, price);

            flightSeat = new FlightSeat
            {
                SeatId = seatId,
                FlightScheduleId = scheduleId,
                Status = FlightSeatStatus.Blocked,
                Price = price,
            };
            db.FlightSeats.Add(flightSeat);
        }
        else
        {
            if (flightSeat.Status is FlightSeatStatus.Booked or FlightSeatStatus.Reserved)
                throw new BusinessRuleException("Cannot block or release a seat that is reserved or booked.");
            flightSeat.Status = status;
            flightSeat.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);

        var evt = status == FlightSeatStatus.Blocked ? "SeatBlocked" : "SeatReleased";
        await seatHub.Clients.Group(scheduleId.ToString())
            .SendAsync(evt, flightSeat.Id, cancellationToken: cancellationToken);

        return new FlightManagerSeatDto(
            seat.Id, flightSeat.Id, seat.SeatNumber, seat.SeatClass, flightSeat.Status, flightSeat.Price);
    }

    public async Task<Ticket?> CheckInTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);
        if (ticket is null) return null;

        switch (ticket.TicketStatus)
        {
            case TicketStatus.CheckedIn:
                throw new BusinessRuleException("Passenger is already checked in.");
            case TicketStatus.Cancelled:
            case TicketStatus.Refunded:
                throw new BusinessRuleException("Cannot check in a cancelled or refunded ticket.");
            case TicketStatus.Used:
                throw new BusinessRuleException("This ticket has already been used.");
        }

        ticket.TicketStatus = TicketStatus.CheckedIn;
        ticket.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ticket;
    }

    public async Task<int?> NotifySchedulePassengersAsync(Guid scheduleId, string title, string message, CancellationToken cancellationToken = default)
    {
        var exists = await db.FlightSchedules.AsNoTracking()
            .AnyAsync(s => s.Id == scheduleId, cancellationToken);
        if (!exists) return null;

        var userIds = await db.Tickets.AsNoTracking()
            .Where(t => t.FlightScheduleId == scheduleId &&
                        t.TicketStatus != TicketStatus.Cancelled &&
                        t.TicketStatus != TicketStatus.Refunded)
            .Select(t => t.Booking.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var userId in userIds)
        {
            db.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = "general",
                IsRead = false,
                RelatedEntityName = "FlightSchedule",
                RelatedEntityId = scheduleId,
                CreatedAt = now,
            });
        }
        await db.SaveChangesAsync(cancellationToken);
        return userIds.Count;
    }
}
