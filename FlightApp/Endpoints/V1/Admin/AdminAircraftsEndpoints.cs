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

        return app;
    }

    private static async Task<IResult> GetAll(IAircraftService aircrafts, CancellationToken cancellationToken)
    {
        var list = await aircrafts.GetAllAsync(cancellationToken);
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
}
