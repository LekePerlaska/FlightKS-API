using FlightKS.Auth;
using FlightKS.Models.Dtos.SeatReservations;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class SeatReservationsEndpoints
{
    public static IEndpointRouteBuilder MapSeatReservationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bookings/{bookingId:guid}/seat-reservations")
            .WithTags("SeatReservations")
            .RequireAuthorization(Policies.User);

        group.MapPost("/", Reserve).WithName("ReserveSeat");
        group.MapDelete("/{flightSeatId:guid}", Release).WithName("ReleaseSeat");

        return app;
    }

    private static async Task<IResult> Reserve(Guid bookingId, SeatReservationCreateDto dto, ICurrentUserAccessor current, ISeatReservationService reservations, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        try
        {
            var result = await reservations.ReserveAsync(
                bookingId, userId.Value, dto.PassengerId, dto.FlightSeatId, dto.HoldFor, cancellationToken);
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
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> Release(Guid bookingId, Guid flightSeatId, ICurrentUserAccessor current, ISeatReservationService reservations, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        var ok = await reservations.ReleaseAsync(bookingId, userId.Value, flightSeatId, cancellationToken);
        return ok ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
