using FlightKS.Models.Dtos.BaggageOptions;
using FlightKS.Validation.Catalog;

namespace FlightKS.UnitTests.Validators.Catalog;

public class BaggageOptionCreateValidatorTests
{
    private readonly BaggageOptionCreateValidator _sut = new();

    private static BaggageOptionCreateDto Valid() => new("Cabin Bag", 7m, 15m, null);

    [Fact]
    public void Valid_Passes() =>
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void ZeroWeightKg_Passes() =>
        _sut.TestValidate(Valid() with { WeightKg = 0m })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void ZeroPrice_Passes() =>
        _sut.TestValidate(Valid() with { Price = 0m })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyName_Fails() =>
        _sut.TestValidate(Valid() with { Name = "" })
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void NameTooLong_Fails() =>
        _sut.TestValidate(Valid() with { Name = new string('a', 201) })
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void NegativeWeightKg_Fails() =>
        _sut.TestValidate(Valid() with { WeightKg = -0.1m })
            .ShouldHaveValidationErrorFor(x => x.WeightKg);

    [Fact]
    public void NegativePrice_Fails() =>
        _sut.TestValidate(Valid() with { Price = -1m })
            .ShouldHaveValidationErrorFor(x => x.Price);

    [Fact]
    public void DescriptionTooLong_Fails() =>
        _sut.TestValidate(Valid() with { Description = new string('x', 501) })
            .ShouldHaveValidationErrorFor(x => x.Description);

    [Fact]
    public void NullDescription_Passes() =>
        _sut.TestValidate(Valid() with { Description = null })
            .ShouldNotHaveAnyValidationErrors();
}

public class BaggageOptionUpdateValidatorTests
{
    private readonly BaggageOptionUpdateValidator _sut = new();

    [Fact]
    public void AllNull_Passes() =>
        _sut.TestValidate(new BaggageOptionUpdateDto(null, null, null, null, null))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyNameWhenSet_Fails() =>
        _sut.TestValidate(new BaggageOptionUpdateDto("", null, null, null, null))
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void NegativeWeightKgWhenSet_Fails() =>
        _sut.TestValidate(new BaggageOptionUpdateDto(null, -1m, null, null, null))
            .ShouldHaveValidationErrorFor(x => x.WeightKg);

    [Fact]
    public void NegativePriceWhenSet_Fails() =>
        _sut.TestValidate(new BaggageOptionUpdateDto(null, null, -1m, null, null))
            .ShouldHaveValidationErrorFor(x => x.Price);
}
