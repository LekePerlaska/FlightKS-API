using FlightKS.Auth;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.FlightManager;
using FlightKS.Models.Dtos.FlightSchedules;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1.FlightManager;

public static class FlightManagerSchedulesEndpoints
{
    public static IEndpointRouteBuilder MapFlightManagerSchedulesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/flight-manager/flight-schedules")
            .WithTags("FlightManagerSchedules")
            .RequireAuthorization(Policies.FlightManager);

        group.MapGet("/", GetAll).WithName("FlightManagerGetSchedules");
        group.MapPatch("/{scheduleId:guid}", Patch).WithName("FlightManagerPatchSchedule");
        group.MapGet("/{scheduleId:guid}/passengers", Passengers).WithName("FlightManagerSchedulePassengers");

        return app;
    }

    private static async Task<IResult> GetAll(ICurrentUserAccessor current, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        var list = await schedules.GetForFlightManagerAsync(userId.Value, cancellationToken);
        return TypedResults.Ok(list.Select(s => s.ToManagerListItem()));
    }

    private static async Task<IResult> Patch(Guid scheduleId, FlightScheduleStatusUpdateDto dto, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var updated = await schedules.UpdateAsync(
            scheduleId, dto.Status, dto.Gate, dto.DelayReason, dto.DepartureTime, dto.ArrivalTime, null, null, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToDetail());
    }

    private static async Task<IResult> Passengers(Guid scheduleId, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var manifest = await schedules.GetManifestAsync(scheduleId, cancellationToken);
        return TypedResults.Ok(manifest.Select(row => new FlightManagerPassengerDto(
            row.Passenger.Id,
            row.Passenger.FirstName,
            row.Passenger.LastName,
            row.Passenger.Nationality,
            row.Passenger.PassportNumber,
            row.Ticket.Id,
            row.Ticket.TicketNumber,
            row.Ticket.TicketStatus,
            row.Ticket.FlightSeat?.Seat.SeatNumber)));
    }
}
