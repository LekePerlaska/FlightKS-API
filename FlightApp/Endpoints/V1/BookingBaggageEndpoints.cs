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

        group.MapGet("/", GetForBooking).WithName("GetBookingBaggage");
        group.MapPost("/", Add).WithName("AddBookingBaggage");
        group.MapPut("/", Update).WithName("UpdateBookingBaggage");
        group.MapDelete("/{bookingBaggageId:guid}", Remove).WithName("RemoveBookingBaggage");

        return app;
    }

    private static async Task<IResult> GetForBooking(Guid bookingId, ICurrentUserAccessor current, IBookingBaggageService baggage, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        var items = await baggage.GetForBookingAsync(bookingId, userId.Value, cancellationToken);
        return TypedResults.Ok(items.Select(item => item.ToResponse()));
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

    private static async Task<IResult> Update(Guid bookingId, BookingBaggageUpdateDto dto, ICurrentUserAccessor current, IBookingBaggageService baggage, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        if (dto.Quantity < 1)
        {
            return TypedResults.BadRequest(new { error = "Quantity must be at least 1." });
        }

        var item = await baggage.UpdateQuantityAsync(
            bookingId, dto.Id, userId.Value, dto.Quantity, cancellationToken);

        return item is null
            ? TypedResults.NotFound(new { error = "Booking baggage item not found." })
            : TypedResults.Ok(item.ToResponse());
    }

    private static async Task<IResult> Remove(Guid bookingId, Guid bookingBaggageId, ICurrentUserAccessor current, IBookingBaggageService baggage, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        var removed = await baggage.RemoveAsync(bookingId, bookingBaggageId, userId.Value, cancellationToken);

        return removed
            ? TypedResults.NoContent()
            : TypedResults.NotFound(new { error = "Booking baggage item not found." });
    }
}
