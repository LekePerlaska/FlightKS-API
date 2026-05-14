using FlightKS.Models.Dtos.Alerts;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints;

public static class PriceAlertEndpoints
{
    private const string RoutePrefix = "/alerts";

    public static IEndpointRouteBuilder MapPriceAlertEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix).WithTags("PriceAlerts");

        group.MapGet("/", GetAll).WithName("GetPriceAlerts");
        group.MapPost("/", Create).WithName("CreatePriceAlert");
        group.MapDelete("/{id:guid}", Deactivate).WithName("DeactivatePriceAlert");

        return app;
    }

    private static IResult? RequireUser(HttpContext ctx, out Guid userId)
    {
        userId = default;
        var header = ctx.Request.Headers["X-User-Id"].ToString();
        if (!Guid.TryParse(header, out userId))
            return TypedResults.Unauthorized();
        return null;
    }

    private static async Task<IResult> GetAll(HttpContext ctx, IPriceAlertService alerts, CancellationToken cancellationToken)
    {
        if (RequireUser(ctx, out var userId) is { } unauth) return unauth;
        var result = await alerts.GetAllForUserAsync(userId, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> Create(PriceAlertCreateDto dto, HttpContext ctx, IPriceAlertService alerts, CancellationToken cancellationToken)
    {
        if (RequireUser(ctx, out var userId) is { } unauth) return unauth;
        var created = await alerts.CreateAsync(userId, dto, cancellationToken);
        return TypedResults.Created($"{RoutePrefix}/{created.Id}", created);
    }

    private static async Task<IResult> Deactivate(Guid id, HttpContext ctx, IPriceAlertService alerts, CancellationToken cancellationToken)
    {
        if (RequireUser(ctx, out var userId) is { } unauth) return unauth;
        var ok = await alerts.DeactivateAsync(userId, id, cancellationToken);
        return ok ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
