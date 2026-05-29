using FlightKS.Auth;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.Aircrafts;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1.Admin;

public static class AdminAircraftsEndpoints
{
    public static IEndpointRouteBuilder MapAdminAircraftsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/aircrafts").WithTags("AdminAircrafts").RequireAuthorization(Policies.Admin);

        group.MapGet("/", GetAll).WithName("AdminGetAircrafts");
        group.MapPost("/", Create).WithName("AdminCreateAircraft");
        group.MapPut("/{id:guid}", Update).WithName("AdminUpdateAircraft");
        group.MapPatch("/{id:guid}", ToggleStatus).WithName("AdminToggleAircraftStatus");
        group.MapDelete("/{id:guid}", Delete).WithName("AdminDeleteAircraft");
        group.MapGet("/{id:guid}/seats", GetSeats).WithName("AdminGetAircraftSeats");
        group.MapPost("/{id:guid}/seats/batch", GenerateSeats).WithName("AdminGenerateAircraftSeats");

        return app;
    }

    private static async Task<IResult> GetAll(IAircraftService aircrafts, CancellationToken cancellationToken)
    {
        var list = await aircrafts.GetAllForAdminAsync(cancellationToken);
        return TypedResults.Ok(list.Select(a => a.ToDto()));
    }

    private static async Task<IResult> Create(AircraftCreateDto dto, IAircraftService aircrafts, CancellationToken cancellationToken)
    {
        try
        {
            var aircraft = await aircrafts.CreateAsync(dto.AirlineId, dto.Model, dto.RegistrationNumber, dto.TotalSeats, cancellationToken);
            return TypedResults.Created($"/api/v1/admin/aircrafts/{aircraft.Id}", aircraft.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> Update(Guid id, AircraftUpdateDto dto, IAircraftService aircrafts, CancellationToken cancellationToken)
    {
        var updated = await aircrafts.UpdateAsync(id, dto.AirlineId, dto.Model, dto.RegistrationNumber, dto.TotalSeats, dto.IsActive, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToDto());
    }

    private static async Task<IResult> ToggleStatus(Guid id, AircraftUpdateDto dto, IAircraftService aircrafts, CancellationToken cancellationToken)
    {
        var updated = await aircrafts.UpdateAsync(id, null, null, null, null, dto.IsActive, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToDto());
    }

    private static async Task<IResult> Delete(Guid id, IAircraftService aircrafts, CancellationToken cancellationToken)
    {
        var deleted = await aircrafts.DeleteAsync(id, cancellationToken);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<IResult> GetSeats(Guid id, IAircraftService aircrafts, CancellationToken cancellationToken)
    {
        var seats = await aircrafts.GetSeatsAsync(id, cancellationToken);
        return TypedResults.Ok(seats.Select(s => s.ToAdminDto()));
    }

    private static async Task<IResult> GenerateSeats(Guid id, SeatBatchCreateDto dto, IAircraftService aircrafts, CancellationToken cancellationToken)
    {
        try
        {
            var seats = await aircrafts.GenerateSeatsAsync(id, dto.Seats, cancellationToken);
            return TypedResults.Ok(seats.Select(s => s.ToAdminDto()));
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new { error = ex.Message });
        }
    }
}
