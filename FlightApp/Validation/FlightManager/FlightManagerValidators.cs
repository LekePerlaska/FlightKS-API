using FlightKS.Models.Dtos.FlightManager;
using FluentValidation;

namespace FlightKS.Validation.FlightManager;

public sealed class NotifyPassengersValidator : AbstractValidator<NotifyPassengersDto>
{
    public NotifyPassengersValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200).When(x => x.Title is not null);
    }
}
