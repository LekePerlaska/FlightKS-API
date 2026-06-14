using FlightKS.Data;
using FlightKS.Enums;
using FlightKS.Models.Dtos.Admin;
using FlightKS.Models.Dtos.FlightManager;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class DashboardService(AppDbContext db) : IDashboardService
{
    private static readonly TimeZoneInfo DashboardTimeZone = GetDashboardTimeZone();

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
        var days = GetLastThirtyDashboardDays();
        var startUtc = ToUtcStart(days[0]);
        var endUtc = ToUtcStart(days[^1].AddDays(1));

        var raw = await db.Payments.AsNoTracking()
            .Where(p => p.PaymentStatus == PaymentStatus.Completed
                     && p.PaidAt.HasValue
                     && p.PaidAt.Value >= startUtc
                     && p.PaidAt.Value < endUtc)
            .Select(p => new { p.PaidAt, p.Amount })
            .ToListAsync(cancellationToken);

        var revenueByDay = raw
            .GroupBy(p => ToDashboardDate(p.PaidAt!.Value))
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        return days
            .Select(day => new RevenueDataPointDto(
                FormatDashboardDate(day),
                revenueByDay.GetValueOrDefault(day)))
            .ToList();
    }

    public async Task<IReadOnlyList<BookingsChartDataPointDto>> GetBookingsChartAsync(CancellationToken cancellationToken = default)
    {
        var days = GetLastThirtyDashboardDays();
        var startUtc = ToUtcStart(days[0]);
        var endUtc = ToUtcStart(days[^1].AddDays(1));

        var raw = await db.Bookings.AsNoTracking()
            .Where(b => b.CreatedAt >= startUtc && b.CreatedAt < endUtc)
            .Select(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        var bookingsByDay = raw
            .GroupBy(ToDashboardDate)
            .ToDictionary(g => g.Key, g => g.Count());

        return days
            .Select(day => new BookingsChartDataPointDto(
                FormatDashboardDate(day),
                bookingsByDay.GetValueOrDefault(day)))
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
        // Scope every count to the manager's assigned airline. With no airline
        // assigned the manager manages nothing, so all counts are zero.
        var airlineId = await db.Users.AsNoTracking()
            .Where(u => u.Id == flightManagerUserId)
            .Select(u => u.AirlineId)
            .FirstOrDefaultAsync(cancellationToken);

        if (airlineId is null)
            return new FlightManagerDashboardSummaryDto(0, 0, 0, 0);

        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);

        var scoped = db.FlightSchedules.AsNoTracking()
            .Where(s => s.Flight.AirlineId == airlineId);

        var todaySchedules = await scoped
            .CountAsync(s => s.DepartureTime >= todayStart && s.DepartureTime < todayEnd, cancellationToken);

        var upcomingSchedules = await scoped
            .CountAsync(s => s.DepartureTime >= todayEnd && s.Status == FlightScheduleStatus.Scheduled, cancellationToken);

        var delayedToday = await scoped
            .CountAsync(s => s.DepartureTime >= todayStart && s.DepartureTime < todayEnd && s.Status == FlightScheduleStatus.Delayed, cancellationToken);

        var cancelledToday = await scoped
            .CountAsync(s => s.DepartureTime >= todayStart && s.DepartureTime < todayEnd && s.Status == FlightScheduleStatus.Cancelled, cancellationToken);

        return new FlightManagerDashboardSummaryDto(todaySchedules, upcomingSchedules, delayedToday, cancelledToday);
    }

    private static TimeZoneInfo GetDashboardTimeZone()
    {
        return TryFindTimeZone("Europe/Budapest")
            ?? TryFindTimeZone("Central Europe Standard Time")
            ?? TimeZoneInfo.Local;
    }

    private static TimeZoneInfo? TryFindTimeZone(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }

    private static IReadOnlyList<DateTime> GetLastThirtyDashboardDays()
    {
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, DashboardTimeZone).Date;
        return Enumerable.Range(0, 30)
            .Select(offset => today.AddDays(offset - 29))
            .ToList();
    }

    private static DateTime ToUtcStart(DateTime localDate) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified), DashboardTimeZone);

    private static DateTime ToDashboardDate(DateTime utcDate)
    {
        var value = utcDate.Kind == DateTimeKind.Utc
            ? utcDate
            : DateTime.SpecifyKind(utcDate, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(value, DashboardTimeZone).Date;
    }

    private static string FormatDashboardDate(DateTime date) => date.ToString("yyyy-MM-dd");
}
