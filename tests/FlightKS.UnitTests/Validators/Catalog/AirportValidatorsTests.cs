using FlightKS.Models.Dtos.Airports;
using FlightKS.Validation.Catalog;

namespace FlightKS.UnitTests.Validators.Catalog;

public class AirportCreateValidatorTests
{
    private readonly AirportCreateValidator _sut = new();

    private static AirportCreateDto Valid() => new("LHR", "Heathrow", "London", "United Kingdom", "Europe/London");

    [Fact]
    public void Valid_Passes() =>
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("America/New_York")]
    [InlineData("Asia/Dubai")]
    [InlineData("Europe/Paris")]
    public void ValidIanaTimezone_Passes(string tz) =>
        _sut.TestValidate(Valid() with { TimeZone = tz })
            .ShouldNotHaveAnyValidationErrors();

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
    public void EmptyCity_Fails() =>
        _sut.TestValidate(Valid() with { City = "" })
            .ShouldHaveValidationErrorFor(x => x.City);

    [Fact]
    public void EmptyCountry_Fails() =>
        _sut.TestValidate(Valid() with { Country = "" })
            .ShouldHaveValidationErrorFor(x => x.Country);

    [Fact]
    public void EmptyTimezone_Fails() =>
        _sut.TestValidate(Valid() with { TimeZone = "" })
            .ShouldHaveValidationErrorFor(x => x.TimeZone);

    [Theory]
    [InlineData("Invalid/Zone")]
    [InlineData("UTC+5")]
    [InlineData("not-a-zone")]
    public void InvalidIanaTimezone_Fails(string tz)
    {
        var result = _sut.TestValidate(Valid() with { TimeZone = tz });
        result.ShouldHaveValidationErrorFor(x => x.TimeZone)
              .WithErrorMessage("TimeZone must be a valid IANA timezone, for example Asia/Dubai or Europe/London.");
    }
}

public class AirportUpdateValidatorTests
{
    private readonly AirportUpdateValidator _sut = new();

    [Fact]
    public void AllNull_Passes() =>
        _sut.TestValidate(new AirportUpdateDto(null, null, null, null, null, null))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void InvalidTimezoneWhenSet_Fails() =>
        _sut.TestValidate(new AirportUpdateDto(null, null, null, null, "Not/A/Zone", null))
            .ShouldHaveValidationErrorFor(x => x.TimeZone);

    [Fact]
    public void ValidTimezoneWhenSet_Passes() =>
        _sut.TestValidate(new AirportUpdateDto(null, null, null, null, "Asia/Tokyo", null))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyCodeWhenSet_Fails() =>
        _sut.TestValidate(new AirportUpdateDto("", null, null, null, null, null))
            .ShouldHaveValidationErrorFor(x => x.Code);

    [Fact]
    public void EmptyTimezoneWhenSet_Fails() =>
        _sut.TestValidate(new AirportUpdateDto(null, null, null, null, "", null))
            .ShouldHaveValidationErrorFor(x => x.TimeZone);
}
