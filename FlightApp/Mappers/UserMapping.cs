using FlightKS.Models.Dtos.Users;
using FlightKS.Models.Entities;

namespace FlightKS.Mappers;

public static class UserMapping
{
    public static UserResponseDto ToResponse(this User user) => new(
        user.Id,
        user.Email,
        user.FullName,
        user.PhoneNumber,
        user.DateOfBirth,
        user.PassportNumber,
        user.Nationality,
        user.IsActive,
        user.CreatedAt,
        user.UpdatedAt,
        [.. user.UserRoles.Select(ur => ur.Role.Name)]);
}
