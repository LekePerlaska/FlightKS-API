using FlightKS.Endpoints.V1;
using FlightKS.Enums;
using FlightKS.Models.Dtos.Payments;
using FlightKS.Validation.Booking;

namespace FlightKS.UnitTests.Validators.Booking;

public class PaymentCreateValidatorTests
{
    private readonly PaymentCreateValidator _sut = new();

    private static PaymentCreateDto Valid() =>
        new(Guid.NewGuid(), 250m, PaymentMethod.Card, null);

    [Fact]
    public void Valid_Passes() =>
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyBookingId_Fails() =>
        _sut.TestValidate(Valid() with { BookingId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.BookingId);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void AmountNotPositive_Fails(double amount) =>
        _sut.TestValidate(Valid() with { Amount = (decimal)amount })
            .ShouldHaveValidationErrorFor(x => x.Amount);
}

public class RefundCreateValidatorTests
{
    private readonly RefundCreateValidator _sut = new();

    private static RefundCreateDto Valid() => new(50m, "Flight cancelled by airline.");

    [Fact]
    public void Valid_Passes() =>
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AmountNotPositive_Fails(double amount) =>
        _sut.TestValidate(Valid() with { Amount = (decimal)amount })
            .ShouldHaveValidationErrorFor(x => x.Amount);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyReason_Fails(string reason) =>
        _sut.TestValidate(Valid() with { Reason = reason })
            .ShouldHaveValidationErrorFor(x => x.Reason);

    [Fact]
    public void ReasonTooLong_Fails() =>
        _sut.TestValidate(Valid() with { Reason = new string('x', 501) })
            .ShouldHaveValidationErrorFor(x => x.Reason);

    [Fact]
    public void ReasonAtMaxLength_Passes() =>
        _sut.TestValidate(Valid() with { Reason = new string('x', 500) })
            .ShouldNotHaveValidationErrorFor(x => x.Reason);
}
