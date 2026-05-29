using FlightKS.Enums;
using FlightKS.Models.Entities;

namespace FlightKS.Services.Interfaces;

public record SeatSummary(int Total, int Available, IDictionary<SeatClass, int> AvailableByClass);

public interface IFlightScheduleService
{
    Task<FlightSchedule?> GetByIdAsync(Guid scheduleId, CancellationToken cancellationToken = default);
    Task<SeatSummary?> GetSeatSummaryAsync(Guid scheduleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<FlightSeat>> GetSeatsAsync(Guid scheduleId, CancellationToken cancellationToken = default);

    Task<IEnumerable<FlightSchedule>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<FlightSchedule> CreateAsync(Guid flightId, Guid aircraftId, DateTime departureTime, DateTime arrivalTime, decimal currentPrice, int availableSeats, string? gate, CancellationToken cancellationToken = default);
    Task<FlightSchedule?> UpdateAsync(Guid scheduleId, FlightScheduleStatus? status, string? gate, string? delayReason, DateTime? departureTime, DateTime? arrivalTime, decimal? currentPrice, int? availableSeats, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid scheduleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<FlightSeat>> GenerateFlightSeatsAsync(Guid scheduleId, CancellationToken cancellationToken = default);

    Task<IEnumerable<FlightSchedule>> GetForFlightManagerAsync(Guid flightManagerUserId, CancellationToken cancellationToken = default);
    Task<IEnumerable<(Passenger Passenger, Ticket Ticket)>> GetManifestAsync(Guid scheduleId, CancellationToken cancellationToken = default);
}
