using FlightKS.Models.Dtos.Admin;
using FlightKS.Models.Dtos.FlightManager;

namespace FlightKS.Services.Interfaces;

public interface IDashboardService
{
    Task<AdminDashboardSummaryDto> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
    Task<FlightManagerDashboardSummaryDto> GetFlightManagerSummaryAsync(Guid flightManagerUserId, CancellationToken cancellationToken = default);
}
