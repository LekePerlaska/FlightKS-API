using FlightKS.Models.Dtos.Airlines;
using FluentValidation;

namespace FlightKS.Validation.Catalog;

public sealed class AirlineCreateValidator : AbstractValidator<AirlineCreateDto>
{
    public AirlineCreateValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
    }
}

public sealed class AirlineUpdateValidator : AbstractValidator<AirlineUpdateDto>
{
    public AirlineUpdateValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10).When(x => x.Code is not null);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100).When(x => x.Country is not null);
    }
}
