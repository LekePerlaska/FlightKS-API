using FlightKS.Models.Dtos.Users;
using FlightKS.Models.Entities;

namespace FlightKS.Mappers;

public static class UserMapping
{
    public static User ToEntity(this UserCreateDto dto, string passwordHash) => new()
    {
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Email = dto.Email,
        PasswordHash = passwordHash,
        PhoneNumber = dto.PhoneNumber,
    };

    public static UserResponseDto ToResponse(this User entity) => new()
    {
        Id = entity.Id,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        Email = entity.Email,
        PhoneNumber = entity.PhoneNumber,
        CreatedAt = entity.CreatedAt,
    };

    public static void ApplyTo(this UserUpdateDto dto, User entity)
    {
        entity.FirstName = dto.FirstName;
        entity.LastName = dto.LastName;
        entity.PhoneNumber = dto.PhoneNumber;
    }
}
