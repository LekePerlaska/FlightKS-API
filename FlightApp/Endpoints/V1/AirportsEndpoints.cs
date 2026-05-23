using FlightKS.Mappers;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class AirportsEndpoints
{
    public static IEndpointRouteBuilder MapAirportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/airports").WithTags("Airports");

        group.MapGet("/", GetAll).WithName("GetAirports");
        group.MapGet("/autocomplete", Autocomplete).WithName("AirportsAutocomplete");

        return app;
    }

    private static async Task<IResult> GetAll(IAirportService airports, CancellationToken cancellationToken)
    {
        var list = await airports.GetAllAsync(cancellationToken);
        return TypedResults.Ok(list.Select(a => a.ToDto()));
    }

    private static async Task<IResult> Autocomplete(string q, IAirportService airports, CancellationToken cancellationToken, int limit = 10)
    {
        var list = await airports.AutocompleteAsync(q, limit, cancellationToken);
        return TypedResults.Ok(list.Select(a => a.ToDto()));
    }
}
