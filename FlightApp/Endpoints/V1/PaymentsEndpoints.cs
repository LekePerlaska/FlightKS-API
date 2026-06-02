using FlightKS.Auth;
using FlightKS.Mappers;
using FlightKS.Middleware;
using FlightKS.Models.Dtos.Payments;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class PaymentsEndpoints
{
    public static IEndpointRouteBuilder MapPaymentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/payments").WithTags("Payments")
            .RequireAuthorization(Policies.User)
            .RequireCurrentUser();

        group.MapPost("/", Create).WithName("CreatePayment");

        return app;
    }

    private static async Task<IResult> Create(PaymentCreateDto dto, HttpContext httpContext, IPaymentService payments, CancellationToken cancellationToken)
    {
        var payment = await payments.CreateAsync(
            dto.BookingId, httpContext.CurrentUserId(), dto.Amount, dto.Method, dto.TransactionId, cancellationToken);
        return TypedResults.Created($"/api/v1/payments/{payment.Id}", payment.ToResponse());
    }
}
