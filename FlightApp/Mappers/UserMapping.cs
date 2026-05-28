using FlightKS.Models.Dtos.Users;
using FlightKS.Models.Entities;

namespace FlightKS.Mappers;

public static class UserMapping
{
    public static UserResponseDto ToResponse(this User user, IReadOnlyList<string>? roles = null) => new(
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
        roles ?? [],
        [.. user.UploadedFiles
            .Where(file => file.RelatedEntityName == "UserPassportDocument")
            .OrderByDescending(file => file.CreatedAt)
            .Select(file => new UserDocumentResponseDto(
                file.Id,
                file.FileName,
                file.OriginalFileName,
                file.ContentType,
                file.SizeBytes,
                file.RelatedEntityName,
                file.RelatedEntityId,
                file.CreatedAt))]);
}
