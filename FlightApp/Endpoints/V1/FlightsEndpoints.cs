using FlightKS.Mappers;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class FlightsEndpoints
{
    public static IEndpointRouteBuilder MapFlightsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/flights").WithTags("Flights");

        group.MapGet("/search", Search).WithName("SearchFlights");

        return app;
    }

    private static async Task<IResult> Search(
        Guid originAirportId,
        Guid destinationAirportId,
        DateOnly date,
        IFlightService flights,
        CancellationToken cancellationToken,
        int passengers = 1)
    {
        var results = await flights.SearchAsync(originAirportId, destinationAirportId, date, passengers, cancellationToken);
        return TypedResults.Ok(results.Select(s => s.ToSearchResult()));
    }
}
