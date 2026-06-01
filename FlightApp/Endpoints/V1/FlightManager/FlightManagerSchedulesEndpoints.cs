using System.Text;
using FlightKS.Auth;
using FlightKS.Enums;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.FlightManager;
using FlightKS.Models.Dtos.FlightSchedules;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1.FlightManager;

public static class FlightManagerSchedulesEndpoints
{
    public static IEndpointRouteBuilder MapFlightManagerSchedulesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/flight-manager/flight-schedules")
            .WithTags("FlightManagerSchedules")
            .RequireAuthorization(Policies.FlightManager);

        group.MapGet("/", GetAll).WithName("FlightManagerGetSchedules");
        group.MapPatch("/{scheduleId:guid}", Patch).WithName("FlightManagerPatchSchedule");
        group.MapGet("/{scheduleId:guid}/passengers", Passengers).WithName("FlightManagerSchedulePassengers");
        group.MapGet("/{scheduleId:guid}/flight-seats", Seats).WithName("FlightManagerScheduleSeats");
        group.MapPatch("/{scheduleId:guid}/flight-seats/{seatId:guid}", SetSeatStatus).WithName("FlightManagerSetSeatStatus");
        group.MapPost("/{scheduleId:guid}/notifications", Notify).WithName("FlightManagerNotifyPassengers");
        group.MapGet("/{scheduleId:guid}/manifest/export", ExportManifest).WithName("FlightManagerExportManifest");

        return app;
    }

    private static async Task<IResult> GetAll(ICurrentUserAccessor current, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        var list = await schedules.GetForFlightManagerAsync(userId.Value, cancellationToken);
        return TypedResults.Ok(list.Select(s => s.ToManagerListItem()));
    }

    private static async Task<IResult> Patch(Guid scheduleId, FlightScheduleStatusUpdateDto dto, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var updated = await schedules.UpdateAsync(
            scheduleId, dto.Status, dto.Gate, dto.DelayReason, dto.DepartureTime, dto.ArrivalTime, null, null, null, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToDetail());
    }

    private static async Task<IResult> Passengers(Guid scheduleId, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var manifest = await schedules.GetManifestAsync(scheduleId, cancellationToken);
        return TypedResults.Ok(manifest.Select(ToPassengerDto));
    }

    private static async Task<IResult> Seats(Guid scheduleId, IFlightManagerService flightManager, CancellationToken cancellationToken)
    {
        var seats = await flightManager.GetSeatsAsync(scheduleId, cancellationToken);
        return TypedResults.Ok(seats);
    }

    private static async Task<IResult> SetSeatStatus(Guid scheduleId, Guid seatId, FlightManagerSeatStatusUpdateDto dto, IFlightManagerService flightManager, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await flightManager.SetSeatStatusAsync(scheduleId, seatId, dto.Status, cancellationToken);
            return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> Notify(Guid scheduleId, NotifyPassengersDto dto, IFlightManagerService flightManager, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Message))
            return TypedResults.BadRequest(new { error = "A message is required." });

        var title = string.IsNullOrWhiteSpace(dto.Title) ? "Flight update" : dto.Title.Trim();
        var notified = await flightManager.NotifySchedulePassengersAsync(scheduleId, title, dto.Message.Trim(), cancellationToken);
        return notified is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new { notified });
    }

    private static async Task<IResult> ExportManifest(Guid scheduleId, IFlightScheduleService schedules, CancellationToken cancellationToken)
    {
        var manifest = (await schedules.GetManifestAsync(scheduleId, cancellationToken)).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("Ticket Number,First Name,Last Name,Nationality,Passport,Seat,Status");
        foreach (var row in manifest)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                Csv(row.Ticket.TicketNumber),
                Csv(row.Passenger.FirstName),
                Csv(row.Passenger.LastName),
                Csv(row.Passenger.Nationality),
                Csv(row.Passenger.PassportNumber),
                Csv(row.Ticket.FlightSeat?.Seat?.SeatNumber),
                Csv(row.Ticket.TicketStatus.ToString()),
            }));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return Results.File(bytes, "text/csv", $"manifest-{scheduleId}.csv");
    }

    private static FlightManagerPassengerDto ToPassengerDto((Passenger Passenger, Ticket Ticket) row) => new(
        row.Passenger.Id,
        row.Passenger.FirstName,
        row.Passenger.LastName,
        row.Passenger.Nationality,
        row.Passenger.PassportNumber,
        row.Ticket.Id,
        row.Ticket.TicketNumber,
        row.Ticket.TicketStatus,
        row.Ticket.FlightSeat?.Seat?.SeatNumber);

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
