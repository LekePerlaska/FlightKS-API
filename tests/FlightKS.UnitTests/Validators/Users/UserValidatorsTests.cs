using FlightKS.Models.Dtos.Admin;
using FlightKS.Models.Dtos.Users;
using FlightKS.Validation.Users;

namespace FlightKS.UnitTests.Validators.Users;

public class UserCreateValidatorTests
{
    private readonly UserCreateValidator _sut = new();

    private static UserCreateDto Valid() => new(
        "Jane Doe", "jane@example.com", "P@ssword1", null, null, null, null);

    [Fact]
    public void Valid_Passes() =>
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyFullName_Fails(string name) =>
        _sut.TestValidate(Valid() with { FullName = name })
            .ShouldHaveValidationErrorFor(x => x.FullName);

    [Fact]
    public void FullNameTooLong_Fails() =>
        _sut.TestValidate(Valid() with { FullName = new string('a', 201) })
            .ShouldHaveValidationErrorFor(x => x.FullName);

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void InvalidEmail_Fails(string email) =>
        _sut.TestValidate(Valid() with { Email = email })
            .ShouldHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void EmailTooLong_Fails() =>
        _sut.TestValidate(Valid() with { Email = new string('a', 315) + "@b.com" })
            .ShouldHaveValidationErrorFor(x => x.Email);

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public void InvalidPassword_Fails(string pwd) =>
        _sut.TestValidate(Valid() with { Password = pwd })
            .ShouldHaveValidationErrorFor(x => x.Password);

    [Fact]
    public void PasswordTooLong_Fails() =>
        _sut.TestValidate(Valid() with { Password = new string('x', 129) })
            .ShouldHaveValidationErrorFor(x => x.Password);

    [Fact]
    public void FutureDateOfBirth_Fails() =>
        _sut.TestValidate(Valid() with { DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) })
            .ShouldHaveValidationErrorFor(x => x.DateOfBirth);

    [Fact]
    public void TodayDateOfBirth_Fails() =>
        _sut.TestValidate(Valid() with { DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow) })
            .ShouldHaveValidationErrorFor(x => x.DateOfBirth);

    [Fact]
    public void PastDateOfBirth_Passes() =>
        _sut.TestValidate(Valid() with { DateOfBirth = new DateOnly(1990, 6, 15) })
            .ShouldNotHaveValidationErrorFor(x => x.DateOfBirth);

    [Fact]
    public void NullDateOfBirth_Passes() =>
        _sut.TestValidate(Valid() with { DateOfBirth = null })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void PhoneNumberTooLong_Fails() =>
        _sut.TestValidate(Valid() with { PhoneNumber = new string('1', 51) })
            .ShouldHaveValidationErrorFor(x => x.PhoneNumber);

    [Fact]
    public void NullPhoneNumber_Passes() =>
        _sut.TestValidate(Valid() with { PhoneNumber = null })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void PassportNumberTooLong_Fails() =>
        _sut.TestValidate(Valid() with { PassportNumber = new string('X', 51) })
            .ShouldHaveValidationErrorFor(x => x.PassportNumber);

    [Fact]
    public void NationalityTooLong_Fails() =>
        _sut.TestValidate(Valid() with { Nationality = new string('A', 101) })
            .ShouldHaveValidationErrorFor(x => x.Nationality);
}

public class UserUpdateValidatorTests
{
    private readonly UserUpdateValidator _sut = new();

    [Fact]
    public void AllNull_Passes() =>
        _sut.TestValidate(new UserUpdateDto(null, null, null, null, null))
            .ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyFullNameWhenSet_Fails(string name) =>
        _sut.TestValidate(new UserUpdateDto(name, null, null, null, null))
            .ShouldHaveValidationErrorFor(x => x.FullName);

    [Fact]
    public void FutureDateOfBirthWhenSet_Fails() =>
        _sut.TestValidate(new UserUpdateDto(null, null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), null, null))
            .ShouldHaveValidationErrorFor(x => x.DateOfBirth);
}

public class AdminUserCreateValidatorTests
{
    private readonly AdminUserCreateValidator _sut = new();

    private static AdminUserCreateDto Valid() => new(
        "Admin User", "admin@example.com", "SecurePass1", null, null, null, null);

    [Fact]
    public void Valid_Passes() =>
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyFullName_Fails() =>
        _sut.TestValidate(Valid() with { FullName = "" })
            .ShouldHaveValidationErrorFor(x => x.FullName);

    [Fact]
    public void ShortPassword_Fails() =>
        _sut.TestValidate(Valid() with { Password = "short" })
            .ShouldHaveValidationErrorFor(x => x.Password);

    [Fact]
    public void FutureDateOfBirth_Fails() =>
        _sut.TestValidate(Valid() with { DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) })
            .ShouldHaveValidationErrorFor(x => x.DateOfBirth);
}

public class AdminUserUpdateValidatorTests
{
    private readonly AdminUserUpdateValidator _sut = new();

    [Fact]
    public void AllNull_Passes() =>
        _sut.TestValidate(new AdminUserUpdateDto(null, null, null, null, null))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyFullNameWhenSet_Fails() =>
        _sut.TestValidate(new AdminUserUpdateDto("", null, null, null, null))
            .ShouldHaveValidationErrorFor(x => x.FullName);

    [Fact]
    public void FutureDateOfBirthWhenSet_Fails() =>
        _sut.TestValidate(new AdminUserUpdateDto(null, null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), null, null))
            .ShouldHaveValidationErrorFor(x => x.DateOfBirth);
}

public class AssignRolesValidatorTests
{
    private readonly AssignRolesValidator _sut = new();

    [Fact]
    public void ValidRoles_Passes() =>
        _sut.TestValidate(new AssignRolesDto(["User", "Admin"]))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void NullRoles_Fails() =>
        _sut.TestValidate(new AssignRolesDto(null!))
            .ShouldHaveValidationErrorFor(x => x.Roles);

    [Fact]
    public void EmptyRoles_Fails() =>
        _sut.TestValidate(new AssignRolesDto([]))
            .ShouldHaveValidationErrorFor(x => x.Roles);

    [Fact]
    public void RolesContainingEmptyString_Fails() =>
        _sut.TestValidate(new AssignRolesDto(["User", ""]))
            .ShouldHaveValidationErrorFor("Roles[1]");
}
