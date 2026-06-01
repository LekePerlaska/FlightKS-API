using FlightKS.Enums;
using FlightKS.Models.Dtos.FlightManager;
using FlightKS.Models.Entities;

namespace FlightKS.Services.Interfaces;

public interface IFlightManagerService
{
    Task<IEnumerable<FlightManagerSeatDto>> GetSeatsAsync(Guid scheduleId, CancellationToken cancellationToken = default);
    Task<FlightManagerSeatDto?> SetSeatStatusAsync(Guid scheduleId, Guid seatId, FlightSeatStatus status, CancellationToken cancellationToken = default);
    Task<Ticket?> CheckInTicketAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<int?> NotifySchedulePassengersAsync(Guid scheduleId, string title, string message, CancellationToken cancellationToken = default);
}
