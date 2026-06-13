using FlightKS.Models.Dtos.Passengers;
using FlightKS.Validation.Booking;

namespace FlightKS.UnitTests.Validators.Booking;

public class PassengerCreateValidatorTests
{
    private readonly PassengerCreateValidator _sut = new();

    private static DateOnly Yesterday => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

    private static PassengerCreateDto Valid() => new("Jane", "Doe", new DateOnly(1990, 5, 15), null, null, null);

    [Fact]
    public void Valid_Passes() =>
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyFirstName_Fails(string name) =>
        _sut.TestValidate(Valid() with { FirstName = name })
            .ShouldHaveValidationErrorFor(x => x.FirstName);

    [Fact]
    public void FirstNameTooLong_Fails() =>
        _sut.TestValidate(Valid() with { FirstName = new string('a', 101) })
            .ShouldHaveValidationErrorFor(x => x.FirstName);

    [Fact]
    public void EmptyLastName_Fails() =>
        _sut.TestValidate(Valid() with { LastName = "" })
            .ShouldHaveValidationErrorFor(x => x.LastName);

    [Fact]
    public void LastNameTooLong_Fails() =>
        _sut.TestValidate(Valid() with { LastName = new string('b', 101) })
            .ShouldHaveValidationErrorFor(x => x.LastName);

    [Fact]
    public void DateOfBirthAtMinBoundary_Fails() =>
        _sut.TestValidate(Valid() with { DateOfBirth = new DateOnly(1900, 1, 1) })
            .ShouldHaveValidationErrorFor(x => x.DateOfBirth);

    [Fact]
    public void FutureDateOfBirth_Fails() =>
        _sut.TestValidate(Valid() with { DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) })
            .ShouldHaveValidationErrorFor(x => x.DateOfBirth);

    [Fact]
    public void TodayDateOfBirth_Fails() =>
        _sut.TestValidate(Valid() with { DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow) })
            .ShouldHaveValidationErrorFor(x => x.DateOfBirth);

    [Fact]
    public void YesterdayDateOfBirth_Passes() =>
        _sut.TestValidate(Valid() with { DateOfBirth = Yesterday })
            .ShouldNotHaveValidationErrorFor(x => x.DateOfBirth);

    [Fact]
    public void GenderTooLong_Fails() =>
        _sut.TestValidate(Valid() with { Gender = new string('x', 21) })
            .ShouldHaveValidationErrorFor(x => x.Gender);

    [Fact]
    public void NullGender_Passes() =>
        _sut.TestValidate(Valid() with { Gender = null })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void PassportNumberTooLong_Fails() =>
        _sut.TestValidate(Valid() with { PassportNumber = new string('P', 51) })
            .ShouldHaveValidationErrorFor(x => x.PassportNumber);

    [Fact]
    public void NationalityTooLong_Fails() =>
        _sut.TestValidate(Valid() with { Nationality = new string('N', 101) })
            .ShouldHaveValidationErrorFor(x => x.Nationality);
}
