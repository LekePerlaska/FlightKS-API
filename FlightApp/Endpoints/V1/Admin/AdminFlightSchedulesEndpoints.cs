using FlightKS.Auth;
using FlightKS.Endpoints;
using FlightKS.Enums;
using FlightKS.Mappers;
using FlightKS.Models.Dtos;
using FlightKS.Models.Dtos.FlightSchedules;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1.Admin;

public static class AdminFlightSchedulesEndpoints
{
    public static IEndpointRouteBuilder MapAdminFlightSchedulesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/flight-schedules").WithTags("AdminFlightSchedules").RequireAuthorization(Policies.Admin);

        group.MapGet("/", GetAll).WithName("AdminGetFlightSchedules");
        group.MapGet("/{id:guid}", GetById).WithName("AdminGetFlightScheduleById");
        group.MapPost("/", Create).WithName("AdminCreateFlightSchedule").WithValidation<FlightScheduleCreateDto>();
        group.MapPut("/{id:guid}", Update).WithName("AdminUpdateFlightSchedule").WithValidation<FlightScheduleUpdateDto>();
        group.MapPatch("/{id:guid}", UpdateStatus).WithName("AdminUpdateFlightScheduleStatus").WithValidation<FlightScheduleUpdateDto>();
        group.MapDelete("/{id:guid}", Delete).WithName("AdminDeleteFlightSchedule");
        group.MapGet("/{id:guid}/seats", GetSeats).WithName("AdminGetFlightScheduleSeats");

        return app;
    }

    private static async Task<IResult> GetAll(
        IFlightScheduleService schedules,
        CancellationToken cancellationToken,
        string? search = null,
        FlightScheduleStatus? status = null,
        int page = 1,
        int pageSize = 20)
    {
        var (items, total) = await schedules.GetAllForAdminAsync(search, status, page, pageSize, cancellationToken);
        return TypedResults.Ok(new PagedResult<FlightScheduleAdminListItemDto>(items.Select(s => s.ToAdminListItem()).ToList(), total, page, pageSize));
    }

    private static async Task<IResult> GetById(Guid id, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var schedule = await schedules.GetByIdAsync(id, cancellationToken);
        return schedule is null ? TypedResults.NotFound() : TypedResults.Ok(schedule.ToDetail());
    }

    private static async Task<IResult> Create(FlightScheduleCreateDto dto, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var schedule = await schedules.CreateAsync(
            dto.FlightId, dto.AircraftId, dto.DepartureTime, dto.ArrivalTime,
            dto.CurrentPrice, dto.Gate, ToPriceMap(dto.ClassPrices), cancellationToken);
        return TypedResults.Created($"/api/v1/admin/flight-schedules/{schedule.Id}", schedule.ToAdminListItem());
    }

    private static async Task<IResult> Update(Guid id, FlightScheduleUpdateDto dto, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var updated = await schedules.UpdateAsync(
            id, dto.Status, dto.Gate, dto.DelayReason, dto.DepartureTime, dto.ArrivalTime,
            dto.CurrentPrice, dto.AvailableSeats, ToPriceMap(dto.ClassPrices), cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToAdminListItem());
    }

    private static async Task<IResult> UpdateStatus(Guid id, FlightScheduleUpdateDto dto, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var updated = await schedules.UpdateAsync(
            id, dto.Status, dto.Gate, dto.DelayReason, dto.DepartureTime, dto.ArrivalTime,
            dto.CurrentPrice, dto.AvailableSeats, ToPriceMap(dto.ClassPrices), cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToAdminListItem());
    }

    private static async Task<IResult> Delete(Guid id, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var deleted = await schedules.DeleteAsync(id, cancellationToken);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<IResult> GetSeats(Guid id, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var seats = await schedules.GetSeatsAsync(id, cancellationToken);
        return TypedResults.Ok(seats.Select(s => s.ToAdminDto()));
    }

    private static IReadOnlyDictionary<SeatClass, decimal>? ToPriceMap(IReadOnlyList<FlightScheduleClassPriceDto>? classPrices) =>
        classPrices is null || classPrices.Count == 0
            ? null
            : classPrices
                .GroupBy(c => c.SeatClass)
                .ToDictionary(g => g.Key, g => g.Last().Price);
}
