using FlightKS.Models.Dtos.Auth;
using FlightKS.Validation.Auth;

namespace FlightKS.UnitTests.Validators.Auth;

public class LogoutValidatorTests
{
    private readonly LogoutValidator _sut = new();

    [Fact]
    public void Valid_Passes()
    {
        var result = _sut.TestValidate(new LogoutDto("some-refresh-token"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyRefreshToken_Fails(string token)
    {
        var result = _sut.TestValidate(new LogoutDto(token));
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }
}
