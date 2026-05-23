using FlightKS.Auth;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.Payments;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class PaymentsEndpoints
{
    public static IEndpointRouteBuilder MapPaymentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/payments").WithTags("Payments").RequireAuthorization(Policies.User);

        group.MapPost("/", Create).WithName("CreatePayment");

        return app;
    }

    private static async Task<IResult> Create(PaymentCreateDto dto, ICurrentUserAccessor current, IPaymentService payments, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        try
        {
            var payment = await payments.CreateAsync(
                dto.BookingId, userId.Value, dto.Amount, dto.Method, dto.TransactionId, cancellationToken);
            return TypedResults.Created($"/api/v1/payments/{payment.Id}", payment.ToResponse());
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new { error = ex.Message });
        }
    }
}
