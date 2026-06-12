using FlightKS.Models.Dtos.FlightManager;
using FlightKS.Validation.FlightManager;

namespace FlightKS.UnitTests.Validators.FlightManager;

public class NotifyPassengersValidatorTests
{
    private readonly NotifyPassengersValidator _sut = new();

    private static NotifyPassengersDto Valid() => new(null, "Your flight has been delayed by 30 minutes.");

    [Fact]
    public void Valid_NullTitle_Passes() =>
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Valid_WithTitle_Passes() =>
        _sut.TestValidate(Valid() with { Title = "Flight Delay Notice" })
            .ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyMessage_Fails(string msg) =>
        _sut.TestValidate(Valid() with { Message = msg })
            .ShouldHaveValidationErrorFor(x => x.Message);

    [Fact]
    public void MessageTooLong_Fails() =>
        _sut.TestValidate(Valid() with { Message = new string('x', 1001) })
            .ShouldHaveValidationErrorFor(x => x.Message);

    [Fact]
    public void MessageAtMaxLength_Passes() =>
        _sut.TestValidate(Valid() with { Message = new string('x', 1000) })
            .ShouldNotHaveValidationErrorFor(x => x.Message);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyTitleWhenSet_Fails(string title) =>
        _sut.TestValidate(Valid() with { Title = title })
            .ShouldHaveValidationErrorFor(x => x.Title);

    [Fact]
    public void TitleTooLong_Fails() =>
        _sut.TestValidate(Valid() with { Title = new string('T', 201) })
            .ShouldHaveValidationErrorFor(x => x.Title);

    [Fact]
    public void TitleAtMaxLength_Passes() =>
        _sut.TestValidate(Valid() with { Title = new string('T', 200) })
            .ShouldNotHaveValidationErrorFor(x => x.Title);
}
