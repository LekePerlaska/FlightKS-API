using FlightKS.Models.Dtos.BaggageOptions;
using FluentValidation;

namespace FlightKS.Validation.Catalog;

public sealed class BaggageOptionCreateValidator : AbstractValidator<BaggageOptionCreateDto>
{
    public BaggageOptionCreateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.WeightKg).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}

public sealed class BaggageOptionUpdateValidator : AbstractValidator<BaggageOptionUpdateDto>
{
    public BaggageOptionUpdateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.WeightKg).GreaterThanOrEqualTo(0).When(x => x.WeightKg.HasValue);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).When(x => x.Price.HasValue);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
