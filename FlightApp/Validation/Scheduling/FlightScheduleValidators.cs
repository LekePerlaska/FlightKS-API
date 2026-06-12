using FlightKS.Models.Dtos.FlightSchedules;
using FluentValidation;

namespace FlightKS.Validation.Scheduling;

public sealed class FlightScheduleCreateValidator : AbstractValidator<FlightScheduleCreateDto>
{
    public FlightScheduleCreateValidator()
    {
        RuleFor(x => x.FlightId).NotEqual(Guid.Empty);
        RuleFor(x => x.AircraftId).NotEqual(Guid.Empty);
        RuleFor(x => x.DepartureTime).NotEqual(default(DateTime));
        RuleFor(x => x.ArrivalTime).NotEqual(default(DateTime));
        RuleFor(x => x.ArrivalTime)
            .GreaterThan(x => x.DepartureTime)
            .WithMessage("Arrival time must be after departure time.");
        RuleFor(x => x.CurrentPrice).GreaterThan(0).When(x => x.CurrentPrice.HasValue);
        RuleForEach(x => x.ClassPrices)
            .ChildRules(cp => cp.RuleFor(c => c.Price).GreaterThan(0).WithMessage("Cabin class price must be greater than zero."))
            .When(x => x.ClassPrices is not null);
    }
}

public sealed class FlightScheduleUpdateValidator : AbstractValidator<FlightScheduleUpdateDto>
{
    public FlightScheduleUpdateValidator()
    {
        RuleFor(x => x.ArrivalTime)
            .Must((dto, arrival) => arrival!.Value > dto.DepartureTime!.Value)
            .WithMessage("Arrival time must be after departure time.")
            .When(x => x.ArrivalTime.HasValue && x.DepartureTime.HasValue);
        RuleFor(x => x.CurrentPrice).GreaterThan(0).When(x => x.CurrentPrice.HasValue);
        RuleForEach(x => x.ClassPrices)
            .ChildRules(cp => cp.RuleFor(c => c.Price).GreaterThan(0).WithMessage("Cabin class price must be greater than zero."))
            .When(x => x.ClassPrices is not null);
    }
}

public sealed class FlightScheduleStatusUpdateValidator : AbstractValidator<FlightScheduleStatusUpdateDto>
{
    public FlightScheduleStatusUpdateValidator()
    {
        RuleFor(x => x.Gate).NotEmpty().When(x => x.Gate is not null);
        RuleFor(x => x.DelayReason).NotEmpty().When(x => x.DelayReason is not null);
        RuleFor(x => x.ArrivalTime)
            .Must((dto, arrival) => arrival!.Value > dto.DepartureTime!.Value)
            .WithMessage("Arrival time must be after departure time.")
            .When(x => x.ArrivalTime.HasValue && x.DepartureTime.HasValue);
    }
}
