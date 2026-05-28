using FlightKS.Data;
using FlightKS.Enums;
using FlightKS.Models.Dtos.Admin;
using FlightKS.Models.Dtos.FlightManager;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class DashboardService(AppDbContext db) : IDashboardService
{
    public async Task<AdminDashboardSummaryDto> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var sevenDaysAgo = now.AddDays(-7);
        var thirtyDaysAgo = now.AddDays(-30);

        var totalUsers = await db.Users.AsNoTracking().CountAsync(cancellationToken);
        var totalBookings = await db.Bookings.AsNoTracking().CountAsync(cancellationToken);

        var bookings7 = await db.Bookings.AsNoTracking()
            .CountAsync(b => b.CreatedAt >= sevenDaysAgo, cancellationToken);
        var bookings30 = await db.Bookings.AsNoTracking()
            .CountAsync(b => b.CreatedAt >= thirtyDaysAgo, cancellationToken);

        var revenue7 = await db.Payments.AsNoTracking()
            .Where(p => p.PaymentStatus == PaymentStatus.Completed && p.PaidAt >= sevenDaysAgo)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;
        var revenue30 = await db.Payments.AsNoTracking()
            .Where(p => p.PaymentStatus == PaymentStatus.Completed && p.PaidAt >= thirtyDaysAgo)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var upcoming = await db.FlightSchedules.AsNoTracking()
            .CountAsync(s => s.Status == FlightScheduleStatus.Scheduled && s.DepartureTime > now, cancellationToken);
        var cancelled = await db.FlightSchedules.AsNoTracking()
            .CountAsync(s => s.Status == FlightScheduleStatus.Cancelled && s.UpdatedAt >= thirtyDaysAgo, cancellationToken);

        return new AdminDashboardSummaryDto(
            totalUsers, totalBookings, bookings7, bookings30,
            revenue7, revenue30, upcoming, cancelled);
    }

    public async Task<IReadOnlyList<RevenueDataPointDto>> GetRevenueChartAsync(CancellationToken cancellationToken = default)
    {
        var thirtyDaysAgo = DateTime.UtcNow.Date.AddDays(-29);

        var raw = await db.Payments.AsNoTracking()
            .Where(p => p.PaymentStatus == PaymentStatus.Completed
                     && p.PaidAt.HasValue
                     && p.PaidAt.Value >= thirtyDaysAgo)
            .GroupBy(p => new
            {
                p.PaidAt!.Value.Year,
                p.PaidAt.Value.Month,
                p.PaidAt.Value.Day,
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Day,
                Revenue = g.Sum(p => p.Amount),
            })
            .OrderBy(g => g.Year).ThenBy(g => g.Month).ThenBy(g => g.Day)
            .ToListAsync(cancellationToken);

        return raw
            .Select(r => new RevenueDataPointDto(
                new DateTime(r.Year, r.Month, r.Day).ToString("yyyy-MM-dd"),
                r.Revenue))
            .ToList();
    }

    public async Task<IReadOnlyList<BookingsChartDataPointDto>> GetBookingsChartAsync(CancellationToken cancellationToken = default)
    {
        var thirtyDaysAgo = DateTime.UtcNow.Date.AddDays(-29);

        var raw = await db.Bookings.AsNoTracking()
            .Where(b => b.CreatedAt >= thirtyDaysAgo)
            .GroupBy(b => new
            {
                b.CreatedAt.Year,
                b.CreatedAt.Month,
                b.CreatedAt.Day,
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Day,
                Count = g.Count(),
            })
            .OrderBy(g => g.Year).ThenBy(g => g.Month).ThenBy(g => g.Day)
            .ToListAsync(cancellationToken);

        return raw
            .Select(r => new BookingsChartDataPointDto(
                new DateTime(r.Year, r.Month, r.Day).ToString("yyyy-MM-dd"),
                r.Count))
            .ToList();
    }

    public async Task<IReadOnlyList<PopularDestinationDto>> GetPopularDestinationsAsync(CancellationToken cancellationToken = default)
    {
        var topGroups = await db.Bookings.AsNoTracking()
            .Where(b => b.ItineraryId.HasValue)
            .Join(db.Itineraries, b => b.ItineraryId, i => i.Id,
                  (b, i) => i.DestinationAirportId)
            .GroupBy(id => id)
            .Select(g => new { AirportId = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        var airportIds = topGroups.Select(g => g.AirportId).ToList();
        var airports = await db.Airports.AsNoTracking()
            .Where(a => airportIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        return topGroups
            .Where(g => airports.ContainsKey(g.AirportId))
            .Select(g => new PopularDestinationDto(
                airports[g.AirportId].Code,
                airports[g.AirportId].City,
                airports[g.AirportId].Country,
                g.Count))
            .ToList();
    }

    public async Task<IReadOnlyList<RecentBookingDto>> GetRecentBookingsAsync(CancellationToken cancellationToken = default)
    {
        return await db.Bookings.AsNoTracking()
            .OrderByDescending(b => b.CreatedAt)
            .Take(10)
            .Select(b => new RecentBookingDto(
                b.Id,
                b.BookingReference,
                b.Status,
                b.TotalAmount,
                b.User.FullName,
                b.User.Email,
                b.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<FlightManagerDashboardSummaryDto> GetFlightManagerSummaryAsync(Guid flightManagerUserId, CancellationToken cancellationToken = default)
    {
        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);

        var todaySchedules = await db.FlightSchedules.AsNoTracking()
            .CountAsync(s => s.DepartureTime >= todayStart && s.DepartureTime < todayEnd, cancellationToken);

        var upcomingSchedules = await db.FlightSchedules.AsNoTracking()
            .CountAsync(s => s.DepartureTime >= todayEnd && s.Status == FlightScheduleStatus.Scheduled, cancellationToken);

        var delayedToday = await db.FlightSchedules.AsNoTracking()
            .CountAsync(s => s.DepartureTime >= todayStart && s.DepartureTime < todayEnd && s.Status == FlightScheduleStatus.Delayed, cancellationToken);

        var cancelledToday = await db.FlightSchedules.AsNoTracking()
            .CountAsync(s => s.DepartureTime >= todayStart && s.DepartureTime < todayEnd && s.Status == FlightScheduleStatus.Cancelled, cancellationToken);

        return new FlightManagerDashboardSummaryDto(todaySchedules, upcomingSchedules, delayedToday, cancelledToday);
    }
}
