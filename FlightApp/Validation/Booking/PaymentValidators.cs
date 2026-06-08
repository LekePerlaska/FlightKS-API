using FlightKS.Endpoints.V1;
using FlightKS.Models.Dtos.Payments;
using FluentValidation;

namespace FlightKS.Validation.Booking;

public sealed class PaymentCreateValidator : AbstractValidator<PaymentCreateDto>
{
    public PaymentCreateValidator()
    {
        RuleFor(x => x.BookingId).NotEqual(Guid.Empty);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public sealed class RefundCreateValidator : AbstractValidator<RefundCreateDto>
{
    public RefundCreateValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
