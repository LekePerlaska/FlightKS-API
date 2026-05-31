using FlightKS.Auth;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class PaymentRefundsEndpoints
{
    public static IEndpointRouteBuilder MapPaymentRefundsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/payments").WithTags("PaymentRefunds").RequireAuthorization(Policies.Admin);

        group.MapPost("/{paymentId:guid}/refunds", CreateRefund).WithName("CreatePaymentRefund");

        return app;
    }

    private static async Task<IResult> CreateRefund(
        Guid paymentId,
        RefundCreateDto dto,
        IPaymentService payments,
        CancellationToken cancellationToken)
    {
        try
        {
            var refund = await payments.CreateRefundAsync(paymentId, dto.Amount, dto.Reason, cancellationToken);
            return TypedResults.Created(
                $"/api/v1/payments/{paymentId}/refunds/{refund.Id}",
                new { refund.Id, refund.PaymentId, refund.Amount, refund.Reason, refund.RefundStatus, refund.CreatedAt });
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new { error = ex.Message });
        }
    }
}

public record RefundCreateDto(decimal Amount, string Reason);
