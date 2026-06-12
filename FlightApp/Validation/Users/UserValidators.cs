using FlightKS.Models.Dtos.Admin;
using FlightKS.Models.Dtos.Users;
using FluentValidation;

namespace FlightKS.Validation.Users;

public sealed class UserCreateValidator : AbstractValidator<UserCreateDto>
{
    public UserCreateValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.PhoneNumber).MaximumLength(50).When(x => x.PhoneNumber is not null);
        RuleFor(x => x.DateOfBirth)
            .Must(d => d!.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth must be in the past.")
            .When(x => x.DateOfBirth.HasValue);
        RuleFor(x => x.PassportNumber).MaximumLength(50).When(x => x.PassportNumber is not null);
        RuleFor(x => x.Nationality).MaximumLength(100).When(x => x.Nationality is not null);
    }
}

public sealed class UserUpdateValidator : AbstractValidator<UserUpdateDto>
{
    public UserUpdateValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200).When(x => x.FullName is not null);
        RuleFor(x => x.PhoneNumber).MaximumLength(50).When(x => x.PhoneNumber is not null);
        RuleFor(x => x.DateOfBirth)
            .Must(d => d!.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth must be in the past.")
            .When(x => x.DateOfBirth.HasValue);
        RuleFor(x => x.PassportNumber).MaximumLength(50).When(x => x.PassportNumber is not null);
        RuleFor(x => x.Nationality).MaximumLength(100).When(x => x.Nationality is not null);
    }
}

public sealed class AdminUserCreateValidator : AbstractValidator<AdminUserCreateDto>
{
    public AdminUserCreateValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.PhoneNumber).MaximumLength(50).When(x => x.PhoneNumber is not null);
        RuleFor(x => x.DateOfBirth)
            .Must(d => d!.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth must be in the past.")
            .When(x => x.DateOfBirth.HasValue);
        RuleFor(x => x.PassportNumber).MaximumLength(50).When(x => x.PassportNumber is not null);
        RuleFor(x => x.Nationality).MaximumLength(100).When(x => x.Nationality is not null);
    }
}

public sealed class AdminUserUpdateValidator : AbstractValidator<AdminUserUpdateDto>
{
    public AdminUserUpdateValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200).When(x => x.FullName is not null);
        RuleFor(x => x.PhoneNumber).MaximumLength(50).When(x => x.PhoneNumber is not null);
        RuleFor(x => x.DateOfBirth)
            .Must(d => d!.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth must be in the past.")
            .When(x => x.DateOfBirth.HasValue);
        RuleFor(x => x.PassportNumber).MaximumLength(50).When(x => x.PassportNumber is not null);
        RuleFor(x => x.Nationality).MaximumLength(100).When(x => x.Nationality is not null);
    }
}

public sealed class AssignRolesValidator : AbstractValidator<AssignRolesDto>
{
    public AssignRolesValidator()
    {
        RuleFor(x => x.Roles).NotNull().NotEmpty();
        RuleForEach(x => x.Roles).NotEmpty().When(x => x.Roles is not null);
    }
}
