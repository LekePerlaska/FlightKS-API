using FlightKS.Auth;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.BookingBaggage;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class BookingBaggageEndpoints
{
    public static IEndpointRouteBuilder MapBookingBaggageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bookings/{bookingId:guid}/baggage")
            .WithTags("BookingBaggage")
            .RequireAuthorization(Policies.User);

        group.MapPost("/", Add).WithName("AddBookingBaggage");

        return app;
    }

    private static async Task<IResult> Add(Guid bookingId, BookingBaggageCreateDto dto, ICurrentUserAccessor current, IBookingBaggageService baggage, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        try
        {
            var item = await baggage.AddAsync(
                bookingId, userId.Value, dto.PassengerId, dto.BaggageOptionId, dto.Quantity, cancellationToken);
            return TypedResults.Created(
                $"/api/v1/bookings/{bookingId}/baggage/{item.Id}",
                item.ToResponse());
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new { error = ex.Message });
        }
    }
}
