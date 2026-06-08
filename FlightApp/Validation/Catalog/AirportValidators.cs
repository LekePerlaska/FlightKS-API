using FlightKS.Models.Dtos.Airports;
using FluentValidation;
using NodaTime;

namespace FlightKS.Validation.Catalog;

public sealed class AirportCreateValidator : AbstractValidator<AirportCreateDto>
{
    public AirportCreateValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TimeZone)
            .NotEmpty()
            .Must(tz => DateTimeZoneProviders.Tzdb.GetZoneOrNull(tz) is not null)
            .WithMessage("TimeZone must be a valid IANA timezone, for example Asia/Dubai or Europe/London.");
    }
}

public sealed class AirportUpdateValidator : AbstractValidator<AirportUpdateDto>
{
    public AirportUpdateValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10).When(x => x.Code is not null);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100).When(x => x.City is not null);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100).When(x => x.Country is not null);
        RuleFor(x => x.TimeZone)
            .NotEmpty()
            .Must(tz => DateTimeZoneProviders.Tzdb.GetZoneOrNull(tz!) is not null)
            .WithMessage("TimeZone must be a valid IANA timezone, for example Asia/Dubai or Europe/London.")
            .When(x => x.TimeZone is not null);
    }
}
