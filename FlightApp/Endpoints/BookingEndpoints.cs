using FlightKS.Models.Dtos.Bookings;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints;

public static class BookingEndpoints
{
    private const string RoutePrefix = "/bookings";

    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix).WithTags("Bookings");

        group.MapGet("/", GetAll).WithName("GetBookings");
        group.MapGet("/{id:guid}", GetById).WithName("GetBookingById");
        group.MapPost("/", Create).WithName("CreateBooking");

        return app;
    }

    // Stub auth: caller supplies their user id via `X-User-Id` until JWT middleware lands.
    private static IResult? RequireUser(HttpContext ctx, out Guid userId)
    {
        userId = default;
        var header = ctx.Request.Headers["X-User-Id"].ToString();
        if (!Guid.TryParse(header, out userId))
            return TypedResults.Unauthorized();
        return null;
    }

    private static async Task<IResult> GetAll(HttpContext ctx, IBookingService bookings, CancellationToken cancellationToken)
    {
        if (RequireUser(ctx, out var userId) is { } unauth) return unauth;
        var result = await bookings.GetAllForUserAsync(userId, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetById(Guid id, HttpContext ctx, IBookingService bookings, CancellationToken cancellationToken)
    {
        if (RequireUser(ctx, out var userId) is { } unauth) return unauth;
        var booking = await bookings.GetByIdAsync(userId, id, cancellationToken);
        return booking is null ? TypedResults.NotFound() : TypedResults.Ok(booking);
    }

    private static async Task<IResult> Create(BookingCreateDto dto, HttpContext ctx, IBookingService bookings, CancellationToken cancellationToken)
    {
        if (RequireUser(ctx, out var userId) is { } unauth) return unauth;
        try
        {
            var created = await bookings.CreateAsync(userId, dto, cancellationToken);
            return TypedResults.Created($"{RoutePrefix}/{created.Id}", created);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new { error = ex.Message });
        }
    }
}
