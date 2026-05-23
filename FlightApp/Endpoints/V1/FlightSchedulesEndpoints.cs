using FlightKS.Mappers;
using FlightKS.Models.Dtos.FlightSchedules;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class FlightSchedulesEndpoints
{
    public static IEndpointRouteBuilder MapFlightSchedulesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/flight-schedules").WithTags("FlightSchedules");

        group.MapGet("/{scheduleId:guid}", GetById).WithName("GetFlightSchedule");
        group.MapGet("/{scheduleId:guid}/seat-summary", GetSeatSummary).WithName("GetFlightScheduleSeatSummary");
        group.MapGet("/{scheduleId:guid}/seats", GetSeats).WithName("GetFlightScheduleSeats");

        return app;
    }

    private static async Task<IResult> GetById(Guid scheduleId, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var schedule = await schedules.GetByIdAsync(scheduleId, cancellationToken);
        return schedule is null ? TypedResults.NotFound() : TypedResults.Ok(schedule.ToDetail());
    }

    private static async Task<IResult> GetSeatSummary(Guid scheduleId, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var summary = await schedules.GetSeatSummaryAsync(scheduleId, cancellationToken);
        return summary is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new SeatSummaryDto(summary.Total, summary.Available, new Dictionary<Enums.SeatClass, int>(summary.AvailableByClass)));
    }

    private static async Task<IResult> GetSeats(Guid scheduleId, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var seats = await schedules.GetSeatsAsync(scheduleId, cancellationToken);
        return TypedResults.Ok(seats.Select(s => s.ToDto()));
    }
}
