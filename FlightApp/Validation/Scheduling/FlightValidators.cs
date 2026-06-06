using FlightKS.Models.Dtos.Flights;
using FluentValidation;

namespace FlightKS.Validation.Scheduling;

public sealed class FlightCreateValidator : AbstractValidator<FlightCreateDto>
{
    public FlightCreateValidator()
    {
        RuleFor(x => x.AirlineId).NotEqual(Guid.Empty);
        RuleFor(x => x.FlightNumber).NotEmpty().MaximumLength(10);
        RuleFor(x => x.OriginAirportId).NotEqual(Guid.Empty);
        RuleFor(x => x.DestinationAirportId).NotEqual(Guid.Empty);
        RuleFor(x => x.DestinationAirportId)
            .NotEqual(x => x.OriginAirportId)
            .WithMessage("Origin and destination airports must differ.");
        RuleFor(x => x.BasePrice).GreaterThan(0);
    }
}

public sealed class FlightUpdateValidator : AbstractValidator<FlightUpdateDto>
{
    public FlightUpdateValidator()
    {
        RuleFor(x => x.AirlineId).NotEqual(Guid.Empty).When(x => x.AirlineId.HasValue);
        RuleFor(x => x.FlightNumber).NotEmpty().MaximumLength(10).When(x => x.FlightNumber is not null);
        RuleFor(x => x.OriginAirportId).NotEqual(Guid.Empty).When(x => x.OriginAirportId.HasValue);
        RuleFor(x => x.DestinationAirportId).NotEqual(Guid.Empty).When(x => x.DestinationAirportId.HasValue);
        RuleFor(x => x.BasePrice).GreaterThan(0).When(x => x.BasePrice.HasValue);
    }
}
