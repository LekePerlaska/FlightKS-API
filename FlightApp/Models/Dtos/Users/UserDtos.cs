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
    IReadOnlyList<string> Roles,
    IReadOnlyList<UserDocumentResponseDto> Documents);

public record UserDocumentResponseDto(
    Guid Id,
    string FileName,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string? RelatedEntityName,
    Guid? RelatedEntityId,
    DateTime CreatedAt);
