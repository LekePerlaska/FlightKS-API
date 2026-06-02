using FlightKS.Auth;
using FlightKS.Exceptions;
using FlightKS.Mappers;
using FlightKS.Middleware;
using FlightKS.Models.Dtos.BookingBaggage;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class BookingBaggageEndpoints
{
    public static IEndpointRouteBuilder MapBookingBaggageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bookings/{bookingId:guid}/baggage")
            .WithTags("BookingBaggage")
            .RequireAuthorization(Policies.User)
            .RequireCurrentUser();

        group.MapGet("/", GetForBooking).WithName("GetBookingBaggage");
        group.MapPost("/", Add).WithName("AddBookingBaggage");
        group.MapPut("/", Update).WithName("UpdateBookingBaggage");
        group.MapDelete("/{bookingBaggageId:guid}", Remove).WithName("RemoveBookingBaggage");

        return app;
    }

    private static async Task<IResult> GetForBooking(Guid bookingId, HttpContext httpContext, IBookingBaggageService baggage, CancellationToken cancellationToken)
    {
        var items = await baggage.GetForBookingAsync(bookingId, httpContext.CurrentUserId(), cancellationToken);
        return TypedResults.Ok(items.Select(item => item.ToResponse()));
    }

    private static async Task<IResult> Add(Guid bookingId, BookingBaggageCreateDto dto, HttpContext httpContext, IBookingBaggageService baggage, CancellationToken cancellationToken)
    {
        var item = await baggage.AddAsync(
            bookingId, httpContext.CurrentUserId(), dto.PassengerId, dto.BaggageOptionId, dto.Quantity, cancellationToken);
        return TypedResults.Created($"/api/v1/bookings/{bookingId}/baggage/{item.Id}", item.ToResponse());
    }

    private static async Task<IResult> Update(Guid bookingId, BookingBaggageUpdateDto dto, HttpContext httpContext, IBookingBaggageService baggage, CancellationToken cancellationToken)
    {
        if (dto.Quantity < 1)
            throw new ValidationException("quantity", "Quantity must be at least 1.");

        var item = await baggage.UpdateQuantityAsync(bookingId, dto.Id, httpContext.CurrentUserId(), dto.Quantity, cancellationToken);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item.ToResponse());
    }

    private static async Task<IResult> Remove(Guid bookingId, Guid bookingBaggageId, HttpContext httpContext, IBookingBaggageService baggage, CancellationToken cancellationToken)
    {
        var removed = await baggage.RemoveAsync(bookingId, bookingBaggageId, httpContext.CurrentUserId(), cancellationToken);
        return removed ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
