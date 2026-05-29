using FlightKS.Auth;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.FlightSchedules;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1.Admin;

public static class AdminFlightSchedulesEndpoints
{
    public static IEndpointRouteBuilder MapAdminFlightSchedulesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/flight-schedules").WithTags("AdminFlightSchedules").RequireAuthorization(Policies.Admin);

        group.MapGet("/", GetAll).WithName("AdminGetFlightSchedules");
        group.MapGet("/{id:guid}", GetById).WithName("AdminGetFlightScheduleById");
        group.MapPost("/", Create).WithName("AdminCreateFlightSchedule");
        group.MapPut("/{id:guid}", Update).WithName("AdminUpdateFlightSchedule");
        group.MapPatch("/{id:guid}", UpdateStatus).WithName("AdminUpdateFlightScheduleStatus");
        group.MapDelete("/{id:guid}", Delete).WithName("AdminDeleteFlightSchedule");
        group.MapGet("/{id:guid}/flight-seats", GetSeats).WithName("AdminGetFlightScheduleSeats");
        group.MapPost("/{id:guid}/flight-seats/batch", GenerateSeats).WithName("AdminGenerateFlightSeats");

        return app;
    }

    private static async Task<IResult> GetAll(IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var list = await schedules.GetAllForAdminAsync(cancellationToken);
        return TypedResults.Ok(list.Select(s => s.ToAdminListItem()));
    }

    private static async Task<IResult> GetById(Guid id, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var schedule = await schedules.GetByIdAsync(id, cancellationToken);
        return schedule is null ? TypedResults.NotFound() : TypedResults.Ok(schedule.ToDetail());
    }

    private static async Task<IResult> Create(FlightScheduleCreateDto dto, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        try
        {
            var schedule = await schedules.CreateAsync(
                dto.FlightId, dto.AircraftId, dto.DepartureTime, dto.ArrivalTime,
                dto.CurrentPrice, dto.AvailableSeats, dto.Gate, cancellationToken);
            return TypedResults.Created($"/api/v1/admin/flight-schedules/{schedule.Id}", schedule.ToAdminListItem());
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> Update(Guid id, FlightScheduleUpdateDto dto, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var updated = await schedules.UpdateAsync(
            id, dto.Status, dto.Gate, dto.DelayReason, dto.DepartureTime, dto.ArrivalTime,
            dto.CurrentPrice, dto.AvailableSeats, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToAdminListItem());
    }

    private static async Task<IResult> UpdateStatus(Guid id, FlightScheduleUpdateDto dto, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var updated = await schedules.UpdateAsync(
            id, dto.Status, dto.Gate, dto.DelayReason, dto.DepartureTime, dto.ArrivalTime,
            dto.CurrentPrice, dto.AvailableSeats, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToAdminListItem());
    }

    private static async Task<IResult> Delete(Guid id, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var deleted = await schedules.DeleteAsync(id, cancellationToken);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<IResult> GetSeats(Guid id, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var seats = await schedules.GetSeatsAsync(id, cancellationToken);
        return TypedResults.Ok(seats.Select(s => s.ToDto()));
    }

    private static async Task<IResult> GenerateSeats(Guid id, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        try
        {
            var seats = await schedules.GenerateFlightSeatsAsync(id, cancellationToken);
            return TypedResults.Ok(seats.Select(s => s.ToDto()));
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new { error = ex.Message });
        }
    }
}
