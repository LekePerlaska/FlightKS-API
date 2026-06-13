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

public class FlightManagerService(AppDbContext db, IHubContext<SeatHub> seatHub, INotificationService notificationService)
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
            .Include(t => t.Booking)
            .Include(t => t.Passenger)
            .Include(t => t.FlightSchedule).ThenInclude(s => s.Flight).ThenInclude(f => f.OriginAirport)
            .Include(t => t.FlightSchedule).ThenInclude(s => s.Flight).ThenInclude(f => f.DestinationAirport)
            .Include(t => t.FlightSeat).ThenInclude(fs => fs!.Seat)
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);
        if (ticket is null) return null;

        if (ticket.Booking.Status != BookingStatus.Confirmed)
            throw new BusinessRuleException("Cannot check in a passenger whose booking has not been confirmed (payment required).");

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

        var passengerName = $"{ticket.Passenger.FirstName} {ticket.Passenger.LastName}";
        var flightNumber = ticket.FlightSchedule.Flight.FlightNumber;
        var origin = ticket.FlightSchedule.Flight.OriginAirport.Code;
        var destination = ticket.FlightSchedule.Flight.DestinationAirport.Code;
        var departure = ticket.FlightSchedule.DepartureTime;
        var seatNumber = ticket.FlightSeat?.Seat?.SeatNumber;

        await notificationService.CreateAsync(ticket.Booking.UserId,
            "Check-In Confirmed",
            $"{passengerName} is checked in for flight {flightNumber} ({origin} → {destination}), departing {departure:dd MMM HH:mm} UTC.",
            "check_in_confirmed",
            relatedEntityName: "Ticket", relatedEntityId: ticket.Id,
            cancellationToken: cancellationToken);

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

        foreach (var userId in userIds)
            await notificationService.CreateAsync(userId, title, message, "general",
                relatedEntityName: "FlightSchedule", relatedEntityId: scheduleId,
                sendEmail: true,
                emailSubject: title,
                emailHtml: EmailTemplates.FlightUpdate(title, message),
                cancellationToken: cancellationToken);

        return userIds.Count;
    }
}
