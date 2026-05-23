using FlightKS.Auth;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.Airlines;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1.Admin;

public static class AdminAirlinesEndpoints
{
    public static IEndpointRouteBuilder MapAdminAirlinesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/airlines").WithTags("AdminAirlines").RequireAuthorization(Policies.Admin);

        group.MapGet("/", GetAll).WithName("AdminGetAirlines");
        group.MapPost("/", Create).WithName("AdminCreateAirline");

        return app;
    }

    private static async Task<IResult> GetAll(IAirlineService airlines, CancellationToken cancellationToken)
    {
        var list = await airlines.GetAllForAdminAsync(cancellationToken);
        return TypedResults.Ok(list.Select(a => a.ToAdminListItem()));
    }

    private static async Task<IResult> Create(AirlineCreateDto dto, IAirlineService airlines, CancellationToken cancellationToken)
    {
        try
        {
            var airline = await airlines.CreateAsync(dto.Code, dto.Name, dto.Country, dto.LogoFileId, cancellationToken);
            return TypedResults.Created($"/api/v1/admin/airlines/{airline.Id}", airline.ToAdminListItem());
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new { error = ex.Message });
        }
    }
}
