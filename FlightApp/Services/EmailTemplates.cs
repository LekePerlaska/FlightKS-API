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
}
