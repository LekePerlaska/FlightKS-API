using FlightKS.Models.Dtos.BookingBaggage;
using FlightKS.Validation.Booking;

namespace FlightKS.UnitTests.Validators.Booking;

public class BookingBaggageCreateValidatorTests
{
    private readonly BookingBaggageCreateValidator _sut = new();

    private static BookingBaggageCreateDto Valid() => new(Guid.NewGuid(), Guid.NewGuid(), 1);

    [Fact]
    public void Valid_Passes() =>
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyPassengerId_Fails() =>
        _sut.TestValidate(Valid() with { PassengerId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.PassengerId);

    [Fact]
    public void EmptyBaggageOptionId_Fails() =>
        _sut.TestValidate(Valid() with { BaggageOptionId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.BaggageOptionId);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void QuantityBelowOne_Fails(int qty) =>
        _sut.TestValidate(Valid() with { Quantity = qty })
            .ShouldHaveValidationErrorFor(x => x.Quantity);

    [Fact]
    public void QuantityOne_Passes() =>
        _sut.TestValidate(Valid() with { Quantity = 1 })
            .ShouldNotHaveAnyValidationErrors();
}

public class BookingBaggageUpdateValidatorTests
{
    private readonly BookingBaggageUpdateValidator _sut = new();

    private static BookingBaggageUpdateDto Valid() => new(Guid.NewGuid(), 2);

    [Fact]
    public void Valid_Passes() =>
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyId_Fails() =>
        _sut.TestValidate(Valid() with { Id = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.Id);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void QuantityBelowOne_Fails(int qty) =>
        _sut.TestValidate(Valid() with { Quantity = qty })
            .ShouldHaveValidationErrorFor(x => x.Quantity);
}
