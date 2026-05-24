namespace FlightKS.Models.Dtos.Users;

public record UserCreateDto(
    string FullName,
    string Email,
    string Password,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? PassportNumber,
    string? Nationality);

public record UserUpdateDto(
    string? FullName,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? PassportNumber,
    string? Nationality);

public record UserResponseDto(
    Guid Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? PassportNumber,
    string? Nationality,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<string> Roles);
