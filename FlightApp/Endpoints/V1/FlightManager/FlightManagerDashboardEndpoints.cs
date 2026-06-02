using FlightKS.Auth;
using FlightKS.Middleware;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1.FlightManager;

public static class FlightManagerDashboardEndpoints
{
    public static IEndpointRouteBuilder MapFlightManagerDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/flight-manager/dashboard")
            .WithTags("FlightManagerDashboard")
            .RequireAuthorization(Policies.FlightManager)
            .RequireCurrentUser();

        group.MapGet("/summary", Summary).WithName("FlightManagerDashboardSummary");

        return app;
    }

    private static async Task<IResult> Summary(HttpContext httpContext, IDashboardService dashboard, CancellationToken cancellationToken)
    {
        var summary = await dashboard.GetFlightManagerSummaryAsync(httpContext.CurrentUserId(), cancellationToken);
        return TypedResults.Ok(summary);
    }
}
