using FlightKS.Models.Dtos.BookingBaggage;
using FluentValidation;

namespace FlightKS.Validation.Booking;

public sealed class BookingBaggageCreateValidator : AbstractValidator<BookingBaggageCreateDto>
{
    public BookingBaggageCreateValidator()
    {
        RuleFor(x => x.PassengerId).NotEqual(Guid.Empty);
        RuleFor(x => x.BaggageOptionId).NotEqual(Guid.Empty);
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(1);
    }
}

public sealed class BookingBaggageUpdateValidator : AbstractValidator<BookingBaggageUpdateDto>
{
    public BookingBaggageUpdateValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(1);
    }
}
