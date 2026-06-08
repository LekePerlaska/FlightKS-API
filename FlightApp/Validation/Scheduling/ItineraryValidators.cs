using FlightKS.Models.Dtos.Itineraries;
using FluentValidation;

namespace FlightKS.Validation.Scheduling;

public sealed class ItineraryCreateValidator : AbstractValidator<ItineraryCreateDto>
{
    public ItineraryCreateValidator()
    {
        RuleFor(x => x.FlightScheduleIds)
            .NotNull()
            .NotEmpty()
            .WithMessage("An itinerary must contain at least one flight schedule.");
        RuleFor(x => x.FlightScheduleIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("An itinerary cannot use the same flight schedule twice.")
            .When(x => x.FlightScheduleIds?.Count > 0);
        RuleForEach(x => x.FlightScheduleIds)
            .NotEqual(Guid.Empty)
            .When(x => x.FlightScheduleIds is not null);
    }
}

public sealed class ItineraryUpdateValidator : AbstractValidator<ItineraryUpdateDto>
{
    public ItineraryUpdateValidator()
    {
        RuleFor(x => x.IsActive).NotNull().WithMessage("isActive is required.");
    }
}

public sealed class ItinerarySegmentCreateValidator : AbstractValidator<ItinerarySegmentCreateDto>
{
    public ItinerarySegmentCreateValidator()
    {
        RuleFor(x => x.FlightScheduleId).NotEqual(Guid.Empty);
        RuleFor(x => x.SegmentOrder).GreaterThanOrEqualTo(1);
        RuleFor(x => x.LayoverMinutesAfterSegment)
            .GreaterThanOrEqualTo(0)
            .When(x => x.LayoverMinutesAfterSegment.HasValue);
    }
}

public sealed class ItinerarySegmentUpdateValidator : AbstractValidator<ItinerarySegmentUpdateDto>
{
    public ItinerarySegmentUpdateValidator()
    {
        RuleFor(x => x.FlightScheduleId).NotEqual(Guid.Empty).When(x => x.FlightScheduleId.HasValue);
        RuleFor(x => x.SegmentOrder).GreaterThanOrEqualTo(1).When(x => x.SegmentOrder.HasValue);
        RuleFor(x => x.LayoverMinutesAfterSegment)
            .GreaterThanOrEqualTo(0)
            .When(x => x.LayoverMinutesAfterSegment.HasValue);
    }
}
