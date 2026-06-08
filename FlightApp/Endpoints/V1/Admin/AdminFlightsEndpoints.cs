using FlightKS.Auth;
using FlightKS.Endpoints;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.Flights;
using FlightKS.Services.Interfaces;
using FlightKS.Models.Dtos;

namespace FlightKS.Endpoints.V1.Admin;

public static class AdminFlightsEndpoints
{
    public static IEndpointRouteBuilder MapAdminFlightsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/flights").WithTags("AdminFlights").RequireAuthorization(Policies.Admin);

        group.MapGet("/", GetAll).WithName("AdminGetFlights");
        group.MapGet("/{id:guid}", GetById).WithName("AdminGetFlightById");
        group.MapPost("/", Create).WithName("AdminCreateFlight").WithValidation<FlightCreateDto>();
        group.MapPut("/{id:guid}", Update).WithName("AdminUpdateFlight").WithValidation<FlightUpdateDto>();
        group.MapPatch("/{id:guid}", ToggleStatus).WithName("AdminToggleFlightStatus").WithValidation<FlightUpdateDto>();

        return app;
    }

    private static async Task<IResult> GetAll(
        IFlightService flights,
        CancellationToken cancellationToken,
        string? search = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 20)
    {
        var (items, total) = await flights.GetAllForAdminAsync(search, isActive, page, pageSize, cancellationToken);
        return TypedResults.Ok(new { items = items.Select(f => f.ToAdminListItem()), total, page, pageSize });
    }

    private static async Task<IResult> GetById(Guid id, IFlightService flights, CancellationToken cancellationToken)
    {
        var flight = await flights.GetByIdAsync(id, cancellationToken);
        return flight is null ? TypedResults.NotFound() : TypedResults.Ok(flight.ToAdminListItem());
    }

    private static async Task<IResult> Create(FlightCreateDto dto, IFlightService flights, CancellationToken cancellationToken)
    {
        var flight = await flights.CreateAsync(
            dto.AirlineId, dto.FlightNumber, dto.OriginAirportId, dto.DestinationAirportId,
            dto.BasePrice, cancellationToken);
        return TypedResults.Created($"/api/v1/admin/flights/{flight.Id}", flight.ToAdminListItem());
    }

    private static async Task<IResult> Update(Guid id, FlightUpdateDto dto, IFlightService flights, CancellationToken cancellationToken)
    {
        var updated = await flights.UpdateAsync(
            id, dto.AirlineId, dto.FlightNumber, dto.OriginAirportId, dto.DestinationAirportId,
            dto.BasePrice, dto.IsActive, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToAdminListItem());
    }

    private static async Task<IResult> ToggleStatus(Guid id, FlightUpdateDto dto, IFlightService flights, CancellationToken cancellationToken)
    {
        var updated = await flights.UpdateAsync(id, null, null, null, null, null, dto.IsActive, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToAdminListItem());
    }
}
