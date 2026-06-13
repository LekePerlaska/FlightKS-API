using FlightKS.Data;
using FlightKS.Enums;
using FlightKS.Exceptions;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class PaymentService(AppDbContext db, INotificationService notificationService) : IPaymentService
{
    public async Task<Payment> CreateAsync(
        Guid bookingId,
        Guid ownerUserId,
        decimal amount,
        PaymentMethod method,
        string? transactionId = null,
        CancellationToken cancellationToken = default)
    {
        var booking = await db.Bookings
            .Include(b => b.User)
            .Include(b => b.Tickets).ThenInclude(t => t.FlightSeat)
            .Include(b => b.BookingBaggage).ThenInclude(bb => bb.BaggageOption)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            ?? throw new NotFoundException($"Booking '{bookingId}' not found.");
        if (booking.UserId != ownerUserId)
            throw new ForbiddenException("You do not have access to this booking.");

        // Compute the authoritative amount server-side — never trust the client-supplied figure alone.
        var seatsTotal = booking.Tickets.Sum(t => t.Price);
        var baggageTotal = booking.BookingBaggage.Sum(bb => bb.BaggageOption is not null ? bb.BaggageOption.Price * bb.Quantity : 0m);
        var grandTotal = seatsTotal + baggageTotal > 0 ? seatsTotal + baggageTotal : booking.TotalAmount;
        var alreadyPaid = booking.Payments
            .Where(p => p.PaymentStatus == PaymentStatus.Completed)
            .Sum(p => p.Amount);
        var outstanding = grandTotal - alreadyPaid;

        if (amount < outstanding)
            throw new BusinessRuleException($"Payment amount {amount:F2} is less than the outstanding balance {outstanding:F2}.");

        var payment = new Payment
        {
            BookingId = bookingId,
            Amount = amount,
            PaymentMethod = method,
            PaymentStatus = PaymentStatus.Completed,
            TransactionId = transactionId,
            PaidAt = DateTime.UtcNow,
        };
        db.Payments.Add(payment);

        foreach (var ticket in booking.Tickets)
        {
            if (ticket.FlightSeat is not null && ticket.FlightSeat.Status == FlightSeatStatus.Reserved)
            {
                ticket.FlightSeat.Status = FlightSeatStatus.Booked;
                ticket.FlightSeat.ReservedUntil = null;
                ticket.FlightSeat.UpdatedAt = DateTime.UtcNow;
            }
        }

        booking.Status = BookingStatus.Confirmed;
        booking.TotalAmount = grandTotal;
        booking.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        await notificationService.CreateAsync(
            booking.UserId,
            "Booking Confirmed",
            $"Your booking {booking.BookingReference} has been confirmed. Total paid: ${booking.TotalAmount:F2}.",
            "booking_confirmed",
            relatedEntityName: "Booking", relatedEntityId: booking.Id,
            sendEmail: true,
            emailSubject: $"Booking Confirmed – {booking.BookingReference}",
            emailHtml: EmailTemplates.BookingConfirmed(booking.User?.FullName ?? booking.UserId.ToString(), booking.BookingReference, booking.TotalAmount),
            cancellationToken: cancellationToken);

        return payment;
    }

    public async Task<Payment?> GetByIdAsync(Guid paymentId, Guid? ownerUserId = null, CancellationToken cancellationToken = default)
    {
        var q = db.Payments.AsNoTracking()
            .Include(p => p.Booking)
            .Include(p => p.Refunds)
            .Where(p => p.Id == paymentId);
        if (ownerUserId is { } uid) q = q.Where(p => p.Booking.UserId == uid);
        return await q.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PaymentRefund> CreateRefundAsync(Guid paymentId, decimal amount, string reason, CancellationToken cancellationToken = default)
    {
        var payment = await db.Payments
            .Include(p => p.Booking).ThenInclude(b => b.User)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            ?? throw new NotFoundException($"Payment '{paymentId}' not found.");

        if (payment.PaymentStatus == PaymentStatus.Refunded)
            throw new BusinessRuleException("Payment has already been refunded.");

        if (payment.PaymentStatus != PaymentStatus.Completed)
            throw new BusinessRuleException("Only completed payments can be refunded.");

        var refund = new PaymentRefund
        {
            PaymentId = paymentId,
            Amount = amount,
            Reason = reason,
            RefundStatus = RefundStatus.Completed,
        };
        db.PaymentRefunds.Add(refund);

        payment.PaymentStatus = PaymentStatus.Refunded;
        payment.UpdatedAt = DateTime.UtcNow;

        if (payment.Booking is not null)
        {
            payment.Booking.Status = BookingStatus.Refunded;
            payment.Booking.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);

        if (payment.Booking is not null)
            await notificationService.CreateAsync(
                payment.Booking.UserId,
                "Refund Processed",
                $"A refund of ${amount:F2} for booking {payment.Booking.BookingReference} has been processed.",
                "payment_refunded",
                relatedEntityName: "Payment", relatedEntityId: payment.Id,
                sendEmail: true,
                emailSubject: $"Refund Processed – {payment.Booking.BookingReference}",
                emailHtml: EmailTemplates.PaymentRefunded(
                    payment.Booking.User?.FullName ?? payment.Booking.UserId.ToString(), payment.Booking.BookingReference, amount, reason),
                cancellationToken: cancellationToken);

        return refund;
    }
}
