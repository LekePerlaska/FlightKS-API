using FlightKS.Enums;

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

public record RevenueDataPointDto(string Date, decimal Revenue);

public record BookingsChartDataPointDto(string Date, int Count);

public record PopularDestinationDto(
    string DestinationCode,
    string DestinationCity,
    string DestinationCountry,
    int BookingCount);

public record RecentBookingDto(
    Guid Id,
    string BookingReference,
    BookingStatus Status,
    decimal TotalAmount,
    string UserFullName,
    string UserEmail,
    DateTime CreatedAt);
