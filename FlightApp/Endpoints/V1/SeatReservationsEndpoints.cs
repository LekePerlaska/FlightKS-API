using FlightKS.Auth;
using FlightKS.Middleware;
using FlightKS.Models.Dtos.SeatReservations;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class SeatReservationsEndpoints
{
    public static IEndpointRouteBuilder MapSeatReservationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bookings/{bookingId:guid}/seat-reservations")
            .WithTags("SeatReservations")
            .RequireAuthorization(Policies.User)
            .RequireCurrentUser();

        group.MapPost("/", Reserve).WithName("ReserveSeat");
        group.MapDelete("/{flightSeatId:guid}", Release).WithName("ReleaseSeat");

        return app;
    }

    private static async Task<IResult> Reserve(Guid bookingId, SeatReservationCreateDto dto, HttpContext httpContext, ISeatReservationService reservations, CancellationToken cancellationToken)
    {
        var result = await reservations.ReserveAsync(
            bookingId, httpContext.CurrentUserId(), dto.PassengerId, dto.SeatId, dto.ItinerarySegmentId, dto.HoldFor, cancellationToken);
        return TypedResults.Ok(new SeatReservationResponseDto(
            result.FlightSeat.Id,
            result.FlightSeat.SeatId,
            result.FlightSeat.Seat.SeatNumber,
            result.FlightSeat.Seat.SeatClass,
            dto.PassengerId,
            result.Ticket.Id,
            result.Ticket.TicketNumber,
            result.FlightSeat.Price,
            result.FlightSeat.Status,
            result.FlightSeat.ReservedUntil));
    }

    private static async Task<IResult> Release(Guid bookingId, Guid flightSeatId, HttpContext httpContext, ISeatReservationService reservations, CancellationToken cancellationToken)
    {
        var ok = await reservations.ReleaseAsync(bookingId, httpContext.CurrentUserId(), flightSeatId, cancellationToken);
        return ok ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
