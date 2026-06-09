using FlightKS.Models.Dtos.Bookings;
using FlightKS.Validation.Booking;

namespace FlightKS.UnitTests.Validators.Booking;

public class BookingCreateValidatorTests
{
    private readonly BookingCreateValidator _sut = new();

    private static BookingCreateDto Valid() => new(Guid.NewGuid(), 1, null);

    [Fact]
    public void Valid_Passes() =>
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyItineraryId_Fails() =>
        _sut.TestValidate(Valid() with { ItineraryId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.ItineraryId);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PassengerCountBelowOne_Fails(int count) =>
        _sut.TestValidate(Valid() with { PassengerCount = count })
            .ShouldHaveValidationErrorFor(x => x.PassengerCount);

    [Fact]
    public void PassengerCountAboveNine_Fails() =>
        _sut.TestValidate(Valid() with { PassengerCount = 10 })
            .ShouldHaveValidationErrorFor(x => x.PassengerCount);

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public void PassengerCountAtBoundary_Passes(int count) =>
        _sut.TestValidate(Valid() with { PassengerCount = count })
            .ShouldNotHaveAnyValidationErrors();
}
