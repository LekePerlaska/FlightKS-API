using FlightKS.Auth;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.Passengers;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class BookingPassengersEndpoints
{
    public static IEndpointRouteBuilder MapBookingPassengersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bookings/{bookingId:guid}/passengers")
            .WithTags("BookingPassengers")
            .RequireAuthorization(Policies.User);

        group.MapPost("/", Add).WithName("AddBookingPassenger");

        return app;
    }

    private static async Task<IResult> Add(Guid bookingId, PassengerCreateDto dto, ICurrentUserAccessor current, IPassengerService passengers, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        try
        {
            var passenger = await passengers.AddAsync(
                bookingId, userId.Value, dto.FirstName, dto.LastName, dto.DateOfBirth,
                dto.Gender, dto.PassportNumber, dto.Nationality, cancellationToken);
            return TypedResults.Created($"/api/v1/bookings/{bookingId}/passengers/{passenger.Id}", passenger.ToResponse());
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new { error = ex.Message });
        }
    }
}
