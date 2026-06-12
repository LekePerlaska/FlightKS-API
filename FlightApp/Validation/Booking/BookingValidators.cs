using FlightKS.Models.Dtos.Bookings;
using FluentValidation;

namespace FlightKS.Validation.Booking;

public sealed class BookingCreateValidator : AbstractValidator<BookingCreateDto>
{
    public BookingCreateValidator()
    {
        RuleFor(x => x.ItineraryId).NotEqual(Guid.Empty);
        RuleFor(x => x.PassengerCount).GreaterThanOrEqualTo(1).LessThanOrEqualTo(9);
    }
}
