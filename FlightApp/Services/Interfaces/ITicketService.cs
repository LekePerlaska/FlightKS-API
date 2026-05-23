using FlightKS.Enums;
using FlightKS.Models.Entities;

namespace FlightKS.Services.Interfaces;

public interface ITicketService
{
    Task<Ticket?> GetByIdAsync(Guid ticketId, Guid? ownerUserId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Ticket>> GetForBookingAsync(Guid bookingId, Guid? ownerUserId = null, CancellationToken cancellationToken = default);
    Task<Ticket?> UpdateStatusAsync(Guid ticketId, TicketStatus status, CancellationToken cancellationToken = default);
}
