namespace FlightKS.Models.Dtos.Admin;

public record AdminDashboardSummaryDto(
    int TotalUsers,
    int TotalBookingsAllTime,
    int BookingsLast7Days,
    int BookingsLast30Days,
    decimal RevenueLast7Days,
    decimal RevenueLast30Days,
    int UpcomingScheduledFlights,
    int CancelledFlightsLast30Days);
