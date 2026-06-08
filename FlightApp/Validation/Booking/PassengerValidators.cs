using FlightKS.Models.Dtos.Passengers;
using FluentValidation;

namespace FlightKS.Validation.Booking;

public sealed class PassengerCreateValidator : AbstractValidator<PassengerCreateDto>
{
    public PassengerCreateValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DateOfBirth)
            .GreaterThan(new DateOnly(1900, 1, 1)).WithMessage("Date of birth is invalid.")
            .LessThan(DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Date of birth must be in the past.");
        RuleFor(x => x.Gender).MaximumLength(20).When(x => x.Gender is not null);
        RuleFor(x => x.PassportNumber).MaximumLength(50).When(x => x.PassportNumber is not null);
        RuleFor(x => x.Nationality).MaximumLength(100).When(x => x.Nationality is not null);
    }
}
