using FlightKS.Models.Dtos.Auth;
using FluentValidation;

namespace FlightKS.Validation.Auth;

public sealed class LogoutValidator : AbstractValidator<LogoutDto>
{
    public LogoutValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
