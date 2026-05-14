using FlightKS.Models.Dtos.Auth;
using FlightKS.Models.Dtos.Users;
using FlightKS.Models.Entities;

namespace FlightKS.Mappers;

public static class UserMapping
{
    public static User ToEntity(this SignUpRequestDto dto, string passwordHash) => new()
    {
        Email = dto.Email,
        PasswordHash = passwordHash,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
    };

    public static UserResponseDto ToResponse(this User entity) => new()
    {
        Id = entity.Id,
        Email = entity.Email,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        Phone = entity.Phone,
        DateOfBirth = entity.DateOfBirth,
        Nationality = entity.Nationality,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        Passport = entity.Passport is null ? null : new UserPassportDto
        {
            PassportNumber = entity.Passport.PassportNumber,
            Nationality = entity.Passport.Nationality,
            ExpiryDate = entity.Passport.ExpiryDate,
        },
        Preferences = entity.Preferences is null ? null : new UserPreferencesDto
        {
            PreferredCabin = entity.Preferences.PreferredCabin,
            PreferredMeal = entity.Preferences.PreferredMeal,
            PreferredSeat = entity.Preferences.PreferredSeat,
            PreferredCurrency = entity.Preferences.PreferredCurrency,
            EmailNotifications = entity.Preferences.EmailNotifications,
            SmsNotifications = entity.Preferences.SmsNotifications,
        },
        Loyalty = entity.Loyalty is null ? null : new UserLoyaltyDto
        {
            FrequentFlyerProgram = entity.Loyalty.FrequentFlyerProgram,
            FrequentFlyerNumber = entity.Loyalty.FrequentFlyerNumber,
        },
    };

    public static void ApplyTo(this UserUpdateDto dto, User entity)
    {
        entity.FirstName = dto.FirstName;
        entity.LastName = dto.LastName;
        entity.Phone = dto.Phone;
        entity.DateOfBirth = dto.DateOfBirth;
        entity.Nationality = dto.Nationality;
        entity.UpdatedAt = DateTime.UtcNow;
    }
}
