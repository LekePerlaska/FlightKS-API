using FlightKS.Models.Dtos.Aircrafts;
using FluentValidation;

namespace FlightKS.Validation.Catalog;

public sealed class SeatCreateItemValidator : AbstractValidator<SeatCreateItemDto>
{
    public SeatCreateItemValidator()
    {
        RuleFor(x => x.SeatNumber).NotEmpty().MaximumLength(10);
    }
}

public sealed class AircraftCreateValidator : AbstractValidator<AircraftCreateDto>
{
    public AircraftCreateValidator()
    {
        RuleFor(x => x.AirlineId).NotEqual(Guid.Empty);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);
        RuleFor(x => x.RegistrationNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.TotalSeats).GreaterThanOrEqualTo(0);
    }
}

public sealed class AircraftUpdateValidator : AbstractValidator<AircraftUpdateDto>
{
    public AircraftUpdateValidator()
    {
        RuleFor(x => x.AirlineId).NotEqual(Guid.Empty).When(x => x.AirlineId.HasValue);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100).When(x => x.Model is not null);
        RuleFor(x => x.RegistrationNumber).NotEmpty().MaximumLength(20).When(x => x.RegistrationNumber is not null);
        RuleFor(x => x.TotalSeats).GreaterThan(0).When(x => x.TotalSeats.HasValue);
    }
}

public sealed class SeatBatchCreateValidator : AbstractValidator<SeatBatchCreateDto>
{
    public SeatBatchCreateValidator()
    {
        RuleFor(x => x.Seats).NotNull().NotEmpty();
        RuleForEach(x => x.Seats)
            .SetValidator(new SeatCreateItemValidator())
            .When(x => x.Seats is not null);
    }
}
