using System.Text;
using FlightKS.Auth;
using FlightKS.Mappers;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class TicketsEndpoints
{
    public static IEndpointRouteBuilder MapTicketsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tickets")
            .WithTags("Tickets")
            .RequireAuthorization(Policies.User);

        group.MapGet("/{ticketId:guid}", GetById).WithName("GetTicket");
        group.MapGet("/{ticketId:guid}/download", Download).WithName("DownloadTicket");

        return app;
    }

    private static async Task<IResult> GetById(Guid ticketId, ICurrentUserAccessor current, ITicketService tickets, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        var ticket = await tickets.GetByIdAsync(ticketId, userId.Value, cancellationToken);
        return ticket is null
            ? TypedResults.NotFound(new { error = "Ticket not found." })
            : TypedResults.Ok(ticket.ToResponse());
    }

    private static async Task<IResult> Download(Guid ticketId, ICurrentUserAccessor current, ITicketService tickets, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        var ticket = await tickets.GetByIdAsync(ticketId, userId.Value, cancellationToken);
        if (ticket is null) return TypedResults.NotFound(new { error = "Ticket not found." });

        var bytes = BuildTicketPdf(ticket);
        var filename = $"ticket-{ticket.TicketNumber}.pdf";

        return Results.File(bytes, "application/pdf", filename);
    }

    private static byte[] BuildTicketPdf(Ticket ticket)
    {
        var passengerName = $"{ticket.Passenger.FirstName} {ticket.Passenger.LastName}";
        var route = $"{ticket.FlightSchedule.Flight.OriginAirport.Code} to {ticket.FlightSchedule.Flight.DestinationAirport.Code}";
        var seat = ticket.FlightSeat?.Seat.SeatNumber ?? "Unassigned";
        var lines = new[]
        {
            "FlightKS Ticket",
            $"Ticket: {ticket.TicketNumber}",
            $"Passenger: {passengerName}",
            $"Flight: {ticket.FlightSchedule.Flight.FlightNumber}",
            $"Route: {route}",
            $"Seat: {seat}",
            $"Status: {ticket.TicketStatus}",
            $"Issued: {ticket.IssuedAt:yyyy-MM-dd HH:mm} UTC",
            $"Price: {ticket.Price:0.00}"
        };

        var textCommands = new StringBuilder();
        textCommands.AppendLine("BT");
        textCommands.AppendLine("/F1 18 Tf");
        textCommands.AppendLine("72 760 Td");

        for (var index = 0; index < lines.Length; index++)
        {
            if (index == 1) textCommands.AppendLine("/F1 12 Tf");
            if (index > 0) textCommands.AppendLine("0 -28 Td");
            textCommands.Append('(').Append(EscapePdfText(lines[index])).AppendLine(") Tj");
        }

        textCommands.AppendLine("ET");
        var stream = textCommands.ToString();

        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream"
        };

        var pdf = new StringBuilder();
        var offsets = new List<int> { 0 };
        pdf.AppendLine("%PDF-1.4");

        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
            pdf.AppendLine($"{i + 1} 0 obj");
            pdf.AppendLine(objects[i]);
            pdf.AppendLine("endobj");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(pdf.ToString());
        pdf.AppendLine("xref");
        pdf.AppendLine($"0 {objects.Length + 1}");
        pdf.AppendLine("0000000000 65535 f ");

        for (var i = 1; i < offsets.Count; i++)
        {
            pdf.AppendLine($"{offsets[i]:D10} 00000 n ");
        }

        pdf.AppendLine("trailer");
        pdf.AppendLine($"<< /Size {objects.Length + 1} /Root 1 0 R >>");
        pdf.AppendLine("startxref");
        pdf.AppendLine(xrefOffset.ToString());
        pdf.AppendLine("%%EOF");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static string EscapePdfText(string value) =>
        value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
