using FlightKS.Auth;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1.Admin;

public static class AdminDashboardEndpoints
{
    public static IEndpointRouteBuilder MapAdminDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/dashboard").WithTags("AdminDashboard").RequireAuthorization(Policies.Admin);

        group.MapGet("/summary", Summary).WithName("AdminDashboardSummary");

        return app;
    }

    private static async Task<IResult> Summary(IDashboardService dashboard, CancellationToken cancellationToken) =>
        TypedResults.Ok(await dashboard.GetAdminSummaryAsync(cancellationToken));
}
