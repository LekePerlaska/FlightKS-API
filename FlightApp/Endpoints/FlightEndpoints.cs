using FlightKS.Models.Dtos.Flights;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints;

public static class FlightEndpoints
{
    private const string RoutePrefix = "/api/flights";

    public static IEndpointRouteBuilder MapFlightEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix).WithTags("Flights");

        group.MapGet("/", GetAll).WithName("GetFlights");
        group.MapGet("/{id:guid}", GetById).WithName("GetFlightById");
        group.MapPost("/", Create).WithName("CreateFlight");
        group.MapPut("/{id:guid}", Update).WithName("UpdateFlight");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteFlight");

        return app;
    }

    private static async Task<IResult> GetAll(IFlightService flights, CancellationToken cancellationToken)
    {
        var result = await flights.GetAllAsync(cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetById(Guid id, IFlightService flights, CancellationToken cancellationToken)
    {
        var flight = await flights.GetByIdAsync(id, cancellationToken);
        return flight is null ? TypedResults.NotFound() : TypedResults.Ok(flight);
    }

    private static async Task<IResult> Create(FlightCreateDto dto, IFlightService flights, CancellationToken cancellationToken)
    {
        var created = await flights.CreateAsync(dto, cancellationToken);
        return TypedResults.Created($"{RoutePrefix}/{created.Id}", created);
    }

    private static async Task<IResult> Update(Guid id, FlightUpdateDto dto, IFlightService flights, CancellationToken cancellationToken)
    {
        var updated = await flights.UpdateAsync(id, dto, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated);
    }

    private static async Task<IResult> Delete(Guid id, IFlightService flights, CancellationToken cancellationToken)
    {
        var deleted = await flights.DeleteAsync(id, cancellationToken);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
