using FlightKS.Auth;
using FlightKS.Enums;
using FlightKS.Mappers;
using FlightKS.Middleware;
using FlightKS.Models.Dtos.Bookings;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class BookingsEndpoints
{
    public static IEndpointRouteBuilder MapBookingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bookings").WithTags("Bookings")
            .RequireAuthorization(Policies.User)
            .RequireCurrentUser();

        group.MapPost("/", Create).WithName("CreateBooking");
        group.MapGet("/my", My).WithName("GetMyBookings");
        group.MapGet("/{bookingId:guid}/summary", Summary).WithName("GetBookingSummary");
        group.MapGet("/{bookingId:guid}/price-summary", PriceSummary).WithName("GetBookingPriceSummary");
        group.MapGet("/{bookingId:guid}/confirmation", Confirmation).WithName("GetBookingConfirmation");
        group.MapGet("/{bookingId:guid}/tickets", Tickets).WithName("GetBookingTickets");
        group.MapPost("/{bookingId:guid}/cancel", Cancel).WithName("CancelBooking");

        return app;
    }

    private static async Task<IResult> Create(BookingCreateDto dto, HttpContext httpContext, IBookingService bookings, CancellationToken cancellationToken)
    {
        var userId = httpContext.CurrentUserId();
        var booking = await bookings.CreateAsync(userId, dto.ItineraryId, dto.PassengerCount, ParseCabinClass(dto.CabinClass), cancellationToken);
        return TypedResults.Created($"/api/v1/bookings/{booking.Id}/summary", booking.ToResponse());
    }

    private static async Task<IResult> My(HttpContext httpContext, IBookingService bookings, CancellationToken cancellationToken)
    {
        var list = await bookings.GetForUserAsync(httpContext.CurrentUserId(), cancellationToken);
        return TypedResults.Ok(list.Select(b => b.ToListItem()));
    }

    private static async Task<IResult> Summary(Guid bookingId, HttpContext httpContext, IBookingService bookings, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetSummaryAsync(bookingId, httpContext.CurrentUserId(), cancellationToken);
        return booking is null ? TypedResults.NotFound() : TypedResults.Ok(booking.ToSummary());
    }

    private static async Task<IResult> PriceSummary(Guid bookingId, HttpContext httpContext, IBookingService bookings, CancellationToken cancellationToken)
    {
        var summary = await bookings.GetPriceSummaryAsync(bookingId, httpContext.CurrentUserId(), cancellationToken);
        return summary is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new BookingPriceSummaryDto(summary.SeatsTotal, summary.BaggageTotal, summary.PaidTotal, summary.GrandTotal));
    }

    private static async Task<IResult> Confirmation(Guid bookingId, HttpContext httpContext, IBookingService bookings, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetConfirmationAsync(bookingId, httpContext.CurrentUserId(), cancellationToken);
        return booking is null ? TypedResults.NotFound() : TypedResults.Ok(booking.ToConfirmation());
    }

    private static async Task<IResult> Tickets(Guid bookingId, HttpContext httpContext, ITicketService tickets, CancellationToken cancellationToken)
    {
        var list = await tickets.GetForBookingAsync(bookingId, httpContext.CurrentUserId(), cancellationToken);
        return TypedResults.Ok(list.Select(t => t.ToResponse()));
    }

    private static async Task<IResult> Cancel(Guid bookingId, HttpContext httpContext, IBookingService bookings, CancellationToken cancellationToken)
    {
        var cancelled = await bookings.CancelAsync(bookingId, httpContext.CurrentUserId(), cancellationToken);
        return cancelled ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static SeatClass? ParseCabinClass(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Replace("_", "").Replace(" ", "");
        return Enum.TryParse<SeatClass>(normalized, ignoreCase: true, out var result) ? result : null;
    }
}
