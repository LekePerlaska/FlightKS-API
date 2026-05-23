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

    public async Task<FlightManagerDashboardSummaryDto> GetFlightManagerSummaryAsync(Guid flightManagerUserId, CancellationToken cancellationToken = default)
    {
        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);

        // TODO: filter by FlightManager assignment once that relationship is modelled.
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
