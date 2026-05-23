using FlightKS.Auth;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.Bookings;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class BookingsEndpoints
{
    public static IEndpointRouteBuilder MapBookingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bookings").WithTags("Bookings").RequireAuthorization(Policies.User);

        group.MapPost("/", Create).WithName("CreateBooking");
        group.MapGet("/my", My).WithName("GetMyBookings");
        group.MapGet("/{bookingId:guid}/summary", Summary).WithName("GetBookingSummary");
        group.MapGet("/{bookingId:guid}/price-summary", PriceSummary).WithName("GetBookingPriceSummary");
        group.MapGet("/{bookingId:guid}/confirmation", Confirmation).WithName("GetBookingConfirmation");
        group.MapGet("/{bookingId:guid}/tickets", Tickets).WithName("GetBookingTickets");

        return app;
    }

    private static async Task<IResult> Create(ICurrentUserAccessor current, IBookingService bookings, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        var booking = await bookings.CreateAsync(userId.Value, cancellationToken);
        return TypedResults.Created($"/api/v1/bookings/{booking.Id}/summary", booking.ToResponse());
    }

    private static async Task<IResult> My(ICurrentUserAccessor current, IBookingService bookings, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        var list = await bookings.GetForUserAsync(userId.Value, cancellationToken);
        return TypedResults.Ok(list.Select(b => b.ToListItem()));
    }

    private static async Task<IResult> Summary(Guid bookingId, ICurrentUserAccessor current, IBookingService bookings, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        var booking = await bookings.GetSummaryAsync(bookingId, userId, cancellationToken);
        return booking is null ? TypedResults.NotFound() : TypedResults.Ok(booking.ToSummary());
    }

    private static async Task<IResult> PriceSummary(Guid bookingId, ICurrentUserAccessor current, IBookingService bookings, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        var summary = await bookings.GetPriceSummaryAsync(bookingId, userId.Value, cancellationToken);
        return summary is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new BookingPriceSummaryDto(summary.SeatsTotal, summary.BaggageTotal, summary.PaidTotal, summary.GrandTotal));
    }

    private static async Task<IResult> Confirmation(Guid bookingId, ICurrentUserAccessor current, IBookingService bookings, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        var booking = await bookings.GetConfirmationAsync(bookingId, userId.Value, cancellationToken);
        return booking is null ? TypedResults.NotFound() : TypedResults.Ok(booking.ToConfirmation());
    }

    private static async Task<IResult> Tickets(Guid bookingId, ICurrentUserAccessor current, ITicketService tickets, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        var list = await tickets.GetForBookingAsync(bookingId, userId, cancellationToken);
        return TypedResults.Ok(list.Select(t => t.ToResponse()));
    }
}
