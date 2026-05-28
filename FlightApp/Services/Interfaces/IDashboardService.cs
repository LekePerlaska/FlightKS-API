using FlightKS.Models.Dtos.Admin;
using FlightKS.Models.Dtos.FlightManager;

namespace FlightKS.Services.Interfaces;

public interface IDashboardService
{
    Task<AdminDashboardSummaryDto> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
    Task<FlightManagerDashboardSummaryDto> GetFlightManagerSummaryAsync(Guid flightManagerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RevenueDataPointDto>> GetRevenueChartAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookingsChartDataPointDto>> GetBookingsChartAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PopularDestinationDto>> GetPopularDestinationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecentBookingDto>> GetRecentBookingsAsync(CancellationToken cancellationToken = default);
}
