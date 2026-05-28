using FlightKS.Auth;
using FlightKS.Data;
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

        group.MapPost("/", Create).WithName("CreateUser");
        group.MapGet("/me", GetMe).WithName("GetCurrentUserProfile").RequireAuthorization();
        group.MapPatch("/me", UpdateMe).WithName("UpdateCurrentUserProfile").RequireAuthorization();
        group.MapPost("/me/documents", UploadMyDocument)
            .WithName("UploadCurrentUserDocument")
            .DisableAntiforgery()
            .RequireAuthorization();
        group.MapGet("/{id:guid}", GetById).WithName("GetUserById");
        group.MapPatch("/{id:guid}", Update).WithName("UpdateUser").RequireAuthorization();

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
        {
            return TypedResults.BadRequest(new { error = "File is required." });
        }

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
        string keycloakUserId;
        try
        {
            keycloakUserId = await keycloak.CreateUserAsync(
                dto.Email, dto.FullName, dto.Password, cancellationToken);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return TypedResults.Conflict(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new { error = ex.Message });
        }

        try
        {
            var user = await users.CreateAsync(
                keycloakUserId, dto.FullName, dto.Email,
                dto.PhoneNumber, dto.DateOfBirth, dto.PassportNumber, dto.Nationality,
                cancellationToken);

            return TypedResults.Created($"/api/v1/users/{user.Id}", user.ToResponse());
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new { error = ex.Message });
        }
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
        IUserService users,
        CancellationToken cancellationToken)
    {
        var updated = await users.UpdateAsync(
            id, dto.FullName, dto.PhoneNumber, dto.DateOfBirth,
            dto.PassportNumber, dto.Nationality, cancellationToken);

        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToResponse());
    }
}
