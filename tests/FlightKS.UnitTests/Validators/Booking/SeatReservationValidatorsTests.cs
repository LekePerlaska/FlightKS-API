using FlightKS.Models.Dtos.SeatReservations;
using FlightKS.Validation.Booking;

namespace FlightKS.UnitTests.Validators.Booking;

public class SeatReservationCreateValidatorTests
{
    private readonly SeatReservationCreateValidator _sut = new();

    private static SeatReservationCreateDto Valid() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null);

    [Fact]
    public void Valid_NoHoldFor_Passes() =>
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyPassengerId_Fails() =>
        _sut.TestValidate(Valid() with { PassengerId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.PassengerId);

    [Fact]
    public void EmptySeatId_Fails() =>
        _sut.TestValidate(Valid() with { SeatId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.SeatId);

    [Fact]
    public void EmptyItinerarySegmentId_Fails() =>
        _sut.TestValidate(Valid() with { ItinerarySegmentId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.ItinerarySegmentId);

    [Fact]
    public void HoldForZero_Fails() =>
        _sut.TestValidate(Valid() with { HoldFor = TimeSpan.Zero })
            .ShouldHaveValidationErrorFor(x => x.HoldFor)
            .WithErrorMessage("Hold duration must be between 1 second and 24 hours.");

    [Fact]
    public void HoldForExactly24Hours_Passes() =>
        _sut.TestValidate(Valid() with { HoldFor = TimeSpan.FromHours(24) })
            .ShouldNotHaveValidationErrorFor(x => x.HoldFor);

    [Fact]
    public void HoldForOver24Hours_Fails() =>
        _sut.TestValidate(Valid() with { HoldFor = TimeSpan.FromHours(24) + TimeSpan.FromSeconds(1) })
            .ShouldHaveValidationErrorFor(x => x.HoldFor);

    [Fact]
    public void HoldForOneSecond_Passes() =>
        _sut.TestValidate(Valid() with { HoldFor = TimeSpan.FromSeconds(1) })
            .ShouldNotHaveValidationErrorFor(x => x.HoldFor);

    [Fact]
    public void NullHoldFor_Passes() =>
        _sut.TestValidate(Valid() with { HoldFor = null })
            .ShouldNotHaveAnyValidationErrors();
}
