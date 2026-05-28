using FlightKS.Auth;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1.Admin;

public static class AdminDashboardEndpoints
{
    public static IEndpointRouteBuilder MapAdminDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/dashboard").WithTags("AdminDashboard").RequireAuthorization(Policies.Admin);

        group.MapGet("/summary", Summary).WithName("AdminDashboardSummary");
        group.MapGet("/revenue", Revenue).WithName("AdminDashboardRevenue");
        group.MapGet("/bookings-chart", BookingsChart).WithName("AdminDashboardBookingsChart");
        group.MapGet("/popular-destinations", PopularDestinations).WithName("AdminDashboardPopularDestinations");
        group.MapGet("/recent-bookings", RecentBookings).WithName("AdminDashboardRecentBookings");

        return app;
    }

    private static async Task<IResult> Summary(IDashboardService dashboard, CancellationToken cancellationToken) =>
        TypedResults.Ok(await dashboard.GetAdminSummaryAsync(cancellationToken));

    private static async Task<IResult> Revenue(IDashboardService dashboard, CancellationToken cancellationToken) =>
        TypedResults.Ok(await dashboard.GetRevenueChartAsync(cancellationToken));

    private static async Task<IResult> BookingsChart(IDashboardService dashboard, CancellationToken cancellationToken) =>
        TypedResults.Ok(await dashboard.GetBookingsChartAsync(cancellationToken));

    private static async Task<IResult> PopularDestinations(IDashboardService dashboard, CancellationToken cancellationToken) =>
        TypedResults.Ok(await dashboard.GetPopularDestinationsAsync(cancellationToken));

    private static async Task<IResult> RecentBookings(IDashboardService dashboard, CancellationToken cancellationToken) =>
        TypedResults.Ok(await dashboard.GetRecentBookingsAsync(cancellationToken));
}
