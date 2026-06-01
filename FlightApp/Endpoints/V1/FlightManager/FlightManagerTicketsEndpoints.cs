using FlightKS.Auth;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1.FlightManager;

public static class FlightManagerTicketsEndpoints
{
    public static IEndpointRouteBuilder MapFlightManagerTicketsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/flight-manager/tickets")
            .WithTags("FlightManagerTickets")
            .RequireAuthorization(Policies.FlightManager);

        group.MapPost("/{ticketId:guid}/check-in", CheckIn).WithName("FlightManagerCheckInTicket");

        return app;
    }

    private static async Task<IResult> CheckIn(Guid ticketId, IFlightManagerService flightManager, CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await flightManager.CheckInTicketAsync(ticketId, cancellationToken);
            return ticket is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(new { ticketId = ticket.Id, status = ticket.TicketStatus });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new { error = ex.Message });
        }
    }
}
