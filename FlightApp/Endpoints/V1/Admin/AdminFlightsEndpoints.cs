using FlightKS.Auth;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.Flights;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1.Admin;

public static class AdminFlightsEndpoints
{
    public static IEndpointRouteBuilder MapAdminFlightsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/flights").WithTags("AdminFlights").RequireAuthorization(Policies.Admin);

        group.MapGet("/", GetAll).WithName("AdminGetFlights");
        group.MapPost("/", Create).WithName("AdminCreateFlight");

        return app;
    }

    private static async Task<IResult> GetAll(IFlightService flights, CancellationToken cancellationToken)
    {
        var list = await flights.GetAllForAdminAsync(cancellationToken);
        return TypedResults.Ok(list.Select(f => f.ToAdminListItem()));
    }

    private static async Task<IResult> Create(FlightCreateDto dto, IFlightService flights, CancellationToken cancellationToken)
    {
        try
        {
            var flight = await flights.CreateAsync(
                dto.AirlineId, dto.FlightNumber, dto.OriginAirportId, dto.DestinationAirportId,
                dto.BasePrice, dto.DurationMinutes, cancellationToken);
            return TypedResults.Created($"/api/v1/admin/flights/{flight.Id}", flight.ToAdminListItem());
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new { error = ex.Message });
        }
    }
}
