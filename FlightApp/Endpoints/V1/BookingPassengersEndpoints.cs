using FlightKS.Auth;
using FlightKS.Endpoints;
using FlightKS.Mappers;
using FlightKS.Middleware;
using FlightKS.Models.Dtos.Passengers;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class BookingPassengersEndpoints
{
    public static IEndpointRouteBuilder MapBookingPassengersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bookings/{bookingId:guid}/passengers")
            .WithTags("BookingPassengers")
            .RequireAuthorization(Policies.User)
            .RequireCurrentUser();

        group.MapPost("/", Add).WithName("AddBookingPassenger").WithValidation<PassengerCreateDto>();
        group.MapPut("/", BulkUpdate).WithName("BulkUpdateBookingPassengers");

        return app;
    }

    private static async Task<IResult> Add(Guid bookingId, PassengerCreateDto dto, HttpContext httpContext, IPassengerService passengers, CancellationToken cancellationToken)
    {
        var passenger = await passengers.AddAsync(
            bookingId, httpContext.CurrentUserId(), dto.FirstName, dto.LastName, dto.DateOfBirth,
            dto.Gender, dto.PassportNumber, dto.Nationality, cancellationToken);
        return TypedResults.Created($"/api/v1/bookings/{bookingId}/passengers/{passenger.Id}", passenger.ToResponse());
    }

    private static async Task<IResult> BulkUpdate(Guid bookingId, PassengerBulkUpdateItemDto[] items, HttpContext httpContext, IPassengerService passengers, CancellationToken cancellationToken)
    {
        var userId = httpContext.CurrentUserId();
        var results = new List<object>();
        foreach (var item in items)
        {
            var updated = await passengers.UpdateAsync(
                bookingId, item.Id, userId,
                item.FirstName, item.LastName, item.DateOfBirth,
                item.Gender, item.PassportNumber, item.Nationality,
                cancellationToken);
            if (updated is null)
                return TypedResults.NotFound();
            results.Add(updated.ToResponse());
        }
        return TypedResults.Ok(results);
    }
}
