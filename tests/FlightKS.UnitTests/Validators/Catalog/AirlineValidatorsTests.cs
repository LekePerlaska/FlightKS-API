using FlightKS.Models.Dtos.Airlines;
using FlightKS.Validation.Catalog;

namespace FlightKS.UnitTests.Validators.Catalog;

public class AirlineCreateValidatorTests
{
    private readonly AirlineCreateValidator _sut = new();

    private static AirlineCreateDto Valid() => new("BA", "British Airways", "United Kingdom", null);

    [Fact]
    public void Valid_Passes() =>
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyCode_Fails(string code) =>
        _sut.TestValidate(Valid() with { Code = code })
            .ShouldHaveValidationErrorFor(x => x.Code);

    [Fact]
    public void CodeTooLong_Fails() =>
        _sut.TestValidate(Valid() with { Code = new string('X', 11) })
            .ShouldHaveValidationErrorFor(x => x.Code);

    [Fact]
    public void EmptyName_Fails() =>
        _sut.TestValidate(Valid() with { Name = "" })
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void EmptyCountry_Fails() =>
        _sut.TestValidate(Valid() with { Country = "" })
            .ShouldHaveValidationErrorFor(x => x.Country);

    [Fact]
    public void NameTooLong_Fails() =>
        _sut.TestValidate(Valid() with { Name = new string('a', 201) })
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void CountryTooLong_Fails() =>
        _sut.TestValidate(Valid() with { Country = new string('a', 101) })
            .ShouldHaveValidationErrorFor(x => x.Country);
}

public class AirlineUpdateValidatorTests
{
    private readonly AirlineUpdateValidator _sut = new();

    [Fact]
    public void AllNull_Passes() =>
        _sut.TestValidate(new AirlineUpdateDto(null, null, null, null, null))
            .ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyCodeWhenSet_Fails(string code) =>
        _sut.TestValidate(new AirlineUpdateDto(code, null, null, null, null))
            .ShouldHaveValidationErrorFor(x => x.Code);

    [Fact]
    public void CodeTooLongWhenSet_Fails() =>
        _sut.TestValidate(new AirlineUpdateDto(new string('X', 11), null, null, null, null))
            .ShouldHaveValidationErrorFor(x => x.Code);

    [Fact]
    public void EmptyNameWhenSet_Fails() =>
        _sut.TestValidate(new AirlineUpdateDto(null, "", null, null, null))
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void EmptyCountryWhenSet_Fails() =>
        _sut.TestValidate(new AirlineUpdateDto(null, null, "", null, null))
            .ShouldHaveValidationErrorFor(x => x.Country);
}
