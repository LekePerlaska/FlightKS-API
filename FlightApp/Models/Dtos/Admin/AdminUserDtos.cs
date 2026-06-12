namespace FlightKS.Models.Dtos.Admin;

public record AdminUserListItemDto(
    Guid Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyList<string> Roles);

public record AdminUserDetailDto(
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

public record AdminUserCreateDto(
    string FullName,
    string Email,
    string Password,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? PassportNumber,
    string? Nationality);

public record AdminUserUpdateDto(
    string? FullName,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? PassportNumber,
    string? Nationality);

public record AssignRolesDto(IReadOnlyList<string> Roles);

public record AdminRoleDto(string Id, string Name);
