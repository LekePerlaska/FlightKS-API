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
        group.MapPost("/", Create).WithName("AdminCreateFlightSchedule");

        return app;
    }

    private static async Task<IResult> GetAll(IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var list = await schedules.GetAllForAdminAsync(cancellationToken);
        return TypedResults.Ok(list.Select(s => s.ToAdminListItem()));
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
}
