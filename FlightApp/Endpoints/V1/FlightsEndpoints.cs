using FlightKS.Mappers;
using FlightKS.Middleware;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class FlightsEndpoints
{
    public static IEndpointRouteBuilder MapFlightsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/flights").WithTags("Flights");

        group.MapGet("/search", Search).WithName("SearchFlights")
            .RequireRateLimiting(RateLimitPartitioning.PublicSearchPolicy);
        group.MapGet("/popular-destinations", GetPopularDestinations).WithName("GetPopularDestinations");
        group.MapGet("/featured", GetFeatured).WithName("GetFeaturedFlights");

        return app;
    }

    private static async Task<IResult> GetPopularDestinations(
        IFlightService flights,
        CancellationToken cancellationToken,
        int limit = 6)
    {
        var results = await flights.PopularDestinationsAsync(limit, cancellationToken);
        return TypedResults.Ok(results.Select(a => a.ToDto()));
    }

    private static async Task<IResult> GetFeatured(
        IItineraryService itineraries,
        CancellationToken cancellationToken,
        int limit = 4)
    {
        var results = await itineraries.GetFeaturedAsync(limit, cancellationToken);
        return TypedResults.Ok(results.Select(i => i.ToSearchResult()));
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
