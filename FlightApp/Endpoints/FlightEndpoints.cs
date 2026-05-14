using FlightKS.Enums;
using FlightKS.Helpers;
using FlightKS.Models.Dtos.Flights;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints;

public static class FlightEndpoints
{
    private const string RoutePrefix = "/flights";

    public static IEndpointRouteBuilder MapFlightEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix).WithTags("Flights");

        group.MapGet("/", Search).WithName("SearchFlights");
        group.MapGet("/{id}", GetById).WithName("GetFlightById");

        return app;
    }

    private static async Task<IResult> Search(
        string origin,
        string destination,
        DateOnly date,
        IFlightService flights,
        CancellationToken cancellationToken,
        DateOnly? returnDate = null,
        string? cabin = null,
        short adults = 1,
        string? tripType = null)
    {
        var query = new FlightSearchQuery
        {
            Origin = origin,
            Destination = destination,
            Date = date,
            ReturnDate = returnDate,
            Cabin = EnumParse.SnakeCase(cabin ?? string.Empty, CabinClass.Economy),
            Adults = adults,
            TripType = EnumParse.SnakeCase(tripType ?? string.Empty, TripType.OneWay),
        };
        var result = await flights.SearchAsync(query, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetById(string id, IFlightService flights, CancellationToken cancellationToken)
    {
        var flight = await flights.GetByIdAsync(id, cancellationToken);
        return flight is null ? TypedResults.NotFound() : TypedResults.Ok(flight);
    }
}
