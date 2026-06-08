using FlightKS.Models.Dtos.SeatReservations;
using FluentValidation;

namespace FlightKS.Validation.Booking;

public sealed class SeatReservationCreateValidator : AbstractValidator<SeatReservationCreateDto>
{
    public SeatReservationCreateValidator()
    {
        RuleFor(x => x.PassengerId).NotEqual(Guid.Empty);
        RuleFor(x => x.SeatId).NotEqual(Guid.Empty);
        RuleFor(x => x.ItinerarySegmentId).NotEqual(Guid.Empty);
        RuleFor(x => x.HoldFor)
            .Must(h => h!.Value > TimeSpan.Zero && h.Value <= TimeSpan.FromHours(24))
            .WithMessage("Hold duration must be between 1 second and 24 hours.")
            .When(x => x.HoldFor.HasValue);
    }
}
