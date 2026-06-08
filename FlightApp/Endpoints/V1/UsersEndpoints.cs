using FlightKS.Auth;
using FlightKS.Data;
using FlightKS.Endpoints;
using FlightKS.Exceptions;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.Users;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FlightKS.Endpoints.V1;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users").WithTags("Users");

        group.MapPost("/", Create).WithName("CreateUser").WithValidation<UserCreateDto>();
        group.MapGet("/me", GetMe).WithName("GetCurrentUserProfile").RequireAuthorization();
        group.MapPatch("/me", UpdateMe).WithName("UpdateCurrentUserProfile").RequireAuthorization().WithValidation<UserUpdateDto>();
        group.MapPost("/me/documents", UploadMyDocument)
            .WithName("UploadCurrentUserDocument")
            .DisableAntiforgery()
            .RequireAuthorization();
        group.MapGet("/{id:guid}", GetById).WithName("GetUserById").RequireAuthorization();
        group.MapPatch("/{id:guid}", Update).WithName("UpdateUser").RequireAuthorization().WithValidation<UserUpdateDto>();

        return app;
    }

    private static async Task<IResult> GetMe(
        ICurrentUserAccessor accessor,
        IUserService users,
        CancellationToken cancellationToken)
    {
        var user = await users.GetOrCreateAsync(
            accessor.KeycloakUserId,
            accessor.Email,
            accessor.FullName,
            cancellationToken);
        return TypedResults.Ok(user.ToResponse(accessor.Roles));
    }

    private static async Task<IResult> UpdateMe(
        UserUpdateDto dto,
        ICurrentUserAccessor accessor,
        IUserService users,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByKeycloakIdAsync(accessor.KeycloakUserId, cancellationToken);
        if (user is null) return TypedResults.NotFound();

        var updated = await users.UpdateAsync(
            user.Id, dto.FullName, dto.PhoneNumber, dto.DateOfBirth,
            dto.PassportNumber, dto.Nationality, cancellationToken);

        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToResponse(accessor.Roles));
    }

    private static async Task<IResult> UploadMyDocument(
        [FromForm] IFormFile file,
        ICurrentUserAccessor accessor,
        IUserService users,
        AppDbContext db,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            throw new ValidationException("file", "File is required.");
        if (file.Length > 10 * 1024 * 1024)
            throw new ValidationException("file", "File size must not exceed 10 MB.");

        var user = await users.GetByKeycloakIdAsync(accessor.KeycloakUserId, cancellationToken);
        if (user is null) return TypedResults.NotFound();

        var extension = Path.GetExtension(file.FileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var relativeDirectory = Path.Combine("uploads", "user-documents", user.Id.ToString());
        var absoluteDirectory = Path.Combine(environment.ContentRootPath, relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);

        var storagePath = Path.Combine(relativeDirectory, storedFileName);
        var absolutePath = Path.Combine(absoluteDirectory, storedFileName);

        await using (var stream = File.Create(absolutePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var uploadedFile = new UploadedFile
        {
            UploadedByUserId = user.Id,
            FileName = storedFileName,
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType,
            SizeBytes = file.Length,
            StoragePath = storagePath,
            RelatedEntityName = "UserPassportDocument",
            RelatedEntityId = user.Id,
        };

        db.UploadedFiles.Add(uploadedFile);
        await db.SaveChangesAsync(cancellationToken);

        var dto = new UserDocumentResponseDto(
            uploadedFile.Id,
            uploadedFile.FileName,
            uploadedFile.OriginalFileName,
            uploadedFile.ContentType,
            uploadedFile.SizeBytes,
            uploadedFile.RelatedEntityName,
            uploadedFile.RelatedEntityId,
            uploadedFile.CreatedAt);

        return TypedResults.Created($"/api/v1/files/{uploadedFile.Id}", dto);
    }

    private static async Task<IResult> Create(
        UserCreateDto dto,
        IKeycloakService keycloak,
        IUserService users,
        CancellationToken cancellationToken)
    {
        var keycloakUserId = await keycloak.CreateUserAsync(dto.Email, dto.FullName, dto.Password, cancellationToken);
        var user = await users.CreateAsync(
            keycloakUserId, dto.FullName, dto.Email,
            dto.PhoneNumber, dto.DateOfBirth, dto.PassportNumber, dto.Nationality,
            cancellationToken);

        return TypedResults.Created($"/api/v1/users/{user.Id}", user.ToResponse());
    }

    private static async Task<IResult> GetById(
        Guid id,
        IUserService users,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(id, cancellationToken);
        return user is null ? TypedResults.NotFound() : TypedResults.Ok(user.ToResponse());
    }

    private static async Task<IResult> Update(
        Guid id,
        UserUpdateDto dto,
        ICurrentUserAccessor accessor,
        IUserService users,
        CancellationToken cancellationToken)
    {
        // Only the profile owner may update their own data; admins use /admin/users/{id}.
        var callerId = await accessor.GetUserIdAsync(cancellationToken);
        if (callerId != id)
            throw new ForbiddenException("You can only update your own profile.");

        var updated = await users.UpdateAsync(
            id, dto.FullName, dto.PhoneNumber, dto.DateOfBirth,
            dto.PassportNumber, dto.Nationality, cancellationToken);

        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToResponse());
    }
}
