using FlightKS.Auth;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1.FlightManager;

public static class FlightManagerDashboardEndpoints
{
    public static IEndpointRouteBuilder MapFlightManagerDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/flight-manager/dashboard")
            .WithTags("FlightManagerDashboard")
            .RequireAuthorization(Policies.FlightManager);

        group.MapGet("/summary", Summary).WithName("FlightManagerDashboardSummary");

        return app;
    }

    private static async Task<IResult> Summary(ICurrentUserAccessor current, IDashboardService dashboard, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();
        var summary = await dashboard.GetFlightManagerSummaryAsync(userId.Value, cancellationToken);
        return TypedResults.Ok(summary);
    }
}
