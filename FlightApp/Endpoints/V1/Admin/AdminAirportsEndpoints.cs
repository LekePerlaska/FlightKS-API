using FlightKS.Auth;
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
        group.MapPost("/", Create).WithName("AdminCreateAirport");

        return app;
    }

    private static async Task<IResult> GetAll(IAirportService airports, CancellationToken cancellationToken)
    {
        var list = await airports.GetAllForAdminAsync(cancellationToken);
        return TypedResults.Ok(list.Select(a => a.ToAdminListItem()));
    }

    private static async Task<IResult> Create(AirportCreateDto dto, IAirportService airports, CancellationToken cancellationToken)
    {
        try
        {
            var airport = await airports.CreateAsync(dto.Code, dto.Name, dto.City, dto.Country, dto.TimeZone, cancellationToken);
            return TypedResults.Created($"/api/v1/admin/airports/{airport.Id}", airport.ToAdminListItem());
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new { error = ex.Message });
        }
    }
}
