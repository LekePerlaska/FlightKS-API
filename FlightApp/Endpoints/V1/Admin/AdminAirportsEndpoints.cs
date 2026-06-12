using FlightKS.Auth;
using FlightKS.Endpoints;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.Airports;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1.Admin;

public static class AdminAirportsEndpoints
{
    public static IEndpointRouteBuilder MapAdminAirportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/airports").WithTags("AdminAirports").RequireAuthorization(Policies.Admin);

        group.MapGet("/", GetAll).WithName("AdminGetAirports");
        group.MapPost("/", Create).WithName("AdminCreateAirport").WithValidation<AirportCreateDto>();
        group.MapPut("/{id:guid}", Update).WithName("AdminUpdateAirport").WithValidation<AirportUpdateDto>();
        group.MapPatch("/{id:guid}", ToggleStatus).WithName("AdminToggleAirportStatus").WithValidation<AirportUpdateDto>();
        group.MapDelete("/{id:guid}", Delete).WithName("AdminDeleteAirport");

        return app;
    }

    private static async Task<IResult> GetAll(IAirportService airports, CancellationToken cancellationToken)
    {
        var list = await airports.GetAllForAdminAsync(cancellationToken);
        return TypedResults.Ok(list.Select(a => a.ToAdminListItem()));
    }

    private static async Task<IResult> Create(AirportCreateDto dto, IAirportService airports, CancellationToken cancellationToken)
    {
        var airport = await airports.CreateAsync(dto.Code, dto.Name, dto.City, dto.Country, dto.TimeZone, cancellationToken);
        return TypedResults.Created($"/api/v1/admin/airports/{airport.Id}", airport.ToAdminListItem());
    }

    private static async Task<IResult> Update(Guid id, AirportUpdateDto dto, IAirportService airports, CancellationToken cancellationToken)
    {
        var updated = await airports.UpdateAsync(id, dto.Code, dto.Name, dto.City, dto.Country, dto.TimeZone, dto.IsActive, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToAdminListItem());
    }

    private static async Task<IResult> ToggleStatus(Guid id, AirportUpdateDto dto, IAirportService airports, CancellationToken cancellationToken)
    {
        var updated = await airports.UpdateAsync(id, null, null, null, null, null, dto.IsActive, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToAdminListItem());
    }

    private static async Task<IResult> Delete(Guid id, IAirportService airports, CancellationToken cancellationToken)
    {
        var deleted = await airports.DeleteAsync(id, cancellationToken);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
