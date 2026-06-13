namespace FlightKS.Services;

public static class EmailTemplates
{
    public static string BookingConfirmed(string userName, string bookingRef, decimal amount) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Arial,sans-serif;color:#222;max-width:600px;margin:0 auto;padding:20px">
          <h2 style="color:#1a56db">Booking Confirmed</h2>
          <p>Hi {userName},</p>
          <p>Your booking <strong>{bookingRef}</strong> has been confirmed and payment received.</p>
          <table style="border-collapse:collapse;width:100%;margin:16px 0">
            <tr><td style="padding:8px;border:1px solid #e5e7eb"><strong>Booking Reference</strong></td>
                <td style="padding:8px;border:1px solid #e5e7eb">{bookingRef}</td></tr>
            <tr><td style="padding:8px;border:1px solid #e5e7eb"><strong>Total Paid</strong></td>
                <td style="padding:8px;border:1px solid #e5e7eb">${amount:F2}</td></tr>
          </table>
          <p>Thank you for choosing FlightKS. Have a great flight!</p>
        </body>
        </html>
        """;

    public static string BookingCancelled(string userName, string bookingRef) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Arial,sans-serif;color:#222;max-width:600px;margin:0 auto;padding:20px">
          <h2 style="color:#e02424">Booking Cancelled</h2>
          <p>Hi {userName},</p>
          <p>Your booking <strong>{bookingRef}</strong> has been cancelled.</p>
          <p>If you believe this was a mistake or need assistance, please contact our support team.</p>
        </body>
        </html>
        """;

    public static string PaymentRefunded(string userName, string bookingRef, decimal amount, string reason) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Arial,sans-serif;color:#222;max-width:600px;margin:0 auto;padding:20px">
          <h2 style="color:#1a56db">Refund Processed</h2>
          <p>Hi {userName},</p>
          <p>A refund for booking <strong>{bookingRef}</strong> has been processed.</p>
          <table style="border-collapse:collapse;width:100%;margin:16px 0">
            <tr><td style="padding:8px;border:1px solid #e5e7eb"><strong>Refund Amount</strong></td>
                <td style="padding:8px;border:1px solid #e5e7eb">${amount:F2}</td></tr>
            <tr><td style="padding:8px;border:1px solid #e5e7eb"><strong>Reason</strong></td>
                <td style="padding:8px;border:1px solid #e5e7eb">{reason}</td></tr>
          </table>
          <p>Please allow 3–5 business days for the refund to appear on your statement.</p>
        </body>
        </html>
        """;

    public static string FlightUpdate(string title, string message) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Arial,sans-serif;color:#222;max-width:600px;margin:0 auto;padding:20px">
          <h2 style="color:#e3a008">{title}</h2>
          <p>{message}</p>
          <p>Please check the FlightKS app for the latest information on your flight.</p>
        </body>
        </html>
        """;

    public static string FlightDelayed(string flightNumber, string origin, string destination, DateTime newDeparture, string? reason) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Arial,sans-serif;color:#222;max-width:600px;margin:0 auto;padding:20px">
          <h2 style="color:#e3a008">Flight Delayed</h2>
          <p>Your flight <strong>{flightNumber}</strong> ({origin} → {destination}) has been delayed.</p>
          <table style="border-collapse:collapse;width:100%;margin:16px 0">
            <tr><td style="padding:8px;border:1px solid #e5e7eb"><strong>New Departure</strong></td>
                <td style="padding:8px;border:1px solid #e5e7eb">{newDeparture:dd MMM yyyy HH:mm} UTC</td></tr>
            {(reason is not null ? $"<tr><td style=\"padding:8px;border:1px solid #e5e7eb\"><strong>Reason</strong></td><td style=\"padding:8px;border:1px solid #e5e7eb\">{reason}</td></tr>" : "")}
          </table>
          <p>We apologise for the inconvenience. Please check the FlightKS app for updates.</p>
        </body>
        </html>
        """;

    public static string FlightCancelled(string flightNumber, string origin, string destination) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Arial,sans-serif;color:#222;max-width:600px;margin:0 auto;padding:20px">
          <h2 style="color:#e02424">Flight Cancelled</h2>
          <p>We regret to inform you that flight <strong>{flightNumber}</strong> ({origin} → {destination}) has been cancelled.</p>
          <p>Please contact our support team or visit the FlightKS app to arrange an alternative.</p>
        </body>
        </html>
        """;

    public static string FlightTimeChanged(string flightNumber, string origin, string destination, DateTime newDeparture) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Arial,sans-serif;color:#222;max-width:600px;margin:0 auto;padding:20px">
          <h2 style="color:#e3a008">Departure Time Changed</h2>
          <p>The departure time for flight <strong>{flightNumber}</strong> ({origin} → {destination}) has been updated.</p>
          <table style="border-collapse:collapse;width:100%;margin:16px 0">
            <tr><td style="padding:8px;border:1px solid #e5e7eb"><strong>New Departure</strong></td>
                <td style="padding:8px;border:1px solid #e5e7eb">{newDeparture:dd MMM yyyy HH:mm} UTC</td></tr>
          </table>
          <p>Please update your travel plans accordingly.</p>
        </body>
        </html>
        """;

    public static string FlightGateChanged(string flightNumber, string origin, string destination, string gate) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Arial,sans-serif;color:#222;max-width:600px;margin:0 auto;padding:20px">
          <h2 style="color:#e3a008">Gate Change</h2>
          <p>The departure gate for flight <strong>{flightNumber}</strong> ({origin} → {destination}) has changed.</p>
          <table style="border-collapse:collapse;width:100%;margin:16px 0">
            <tr><td style="padding:8px;border:1px solid #e5e7eb"><strong>New Gate</strong></td>
                <td style="padding:8px;border:1px solid #e5e7eb">{gate}</td></tr>
          </table>
          <p>Please proceed to the correct gate. Check the FlightKS app for the latest information.</p>
        </body>
        </html>
        """;

    public static string CheckInConfirmed(string passengerName, string flightNumber, string origin, string destination, DateTime departure, string? seatNumber) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Arial,sans-serif;color:#222;max-width:600px;margin:0 auto;padding:20px">
          <h2 style="color:#057a55">Check-In Confirmed</h2>
          <p>Hi {passengerName}, you are checked in for your flight!</p>
          <table style="border-collapse:collapse;width:100%;margin:16px 0">
            <tr><td style="padding:8px;border:1px solid #e5e7eb"><strong>Flight</strong></td>
                <td style="padding:8px;border:1px solid #e5e7eb">{flightNumber}</td></tr>
            <tr><td style="padding:8px;border:1px solid #e5e7eb"><strong>Route</strong></td>
                <td style="padding:8px;border:1px solid #e5e7eb">{origin} → {destination}</td></tr>
            <tr><td style="padding:8px;border:1px solid #e5e7eb"><strong>Departure</strong></td>
                <td style="padding:8px;border:1px solid #e5e7eb">{departure:dd MMM yyyy HH:mm} UTC</td></tr>
            {(seatNumber is not null ? $"<tr><td style=\"padding:8px;border:1px solid #e5e7eb\"><strong>Seat</strong></td><td style=\"padding:8px;border:1px solid #e5e7eb\">{seatNumber}</td></tr>" : "")}
          </table>
          <p>Have a wonderful flight!</p>
        </body>
        </html>
        """;

    public static string TicketCancelled(string flightNumber, string origin, string destination) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Arial,sans-serif;color:#222;max-width:600px;margin:0 auto;padding:20px">
          <h2 style="color:#e02424">Ticket Cancelled</h2>
          <p>Your ticket for flight <strong>{flightNumber}</strong> ({origin} → {destination}) has been cancelled.</p>
          <p>If you have questions, please contact our support team.</p>
        </body>
        </html>
        """;
}
