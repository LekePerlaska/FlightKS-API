using FlightKS.Auth;
using FlightKS.Data;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.Airlines;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Endpoints.V1.Admin;

public static class AdminAirlinesEndpoints
{
    public static IEndpointRouteBuilder MapAdminAirlinesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/airlines").WithTags("AdminAirlines").RequireAuthorization(Policies.Admin);

        group.MapGet("/", GetAll).WithName("AdminGetAirlines");
        group.MapPost("/", Create).WithName("AdminCreateAirline");
        group.MapPut("/{id:guid}", Update).WithName("AdminUpdateAirline");
        group.MapPatch("/{id:guid}", ToggleStatus).WithName("AdminToggleAirlineStatus");
        group.MapDelete("/{id:guid}", Delete).WithName("AdminDeleteAirline");
        group.MapPatch("/{id:guid}/restore", Restore).WithName("AdminRestoreAirline");
        group.MapPost("/{id:guid}/logo", UploadLogo).WithName("AdminUploadAirlineLogo").DisableAntiforgery();
        group.MapDelete("/{id:guid}/logo", DeleteLogo).WithName("AdminDeleteAirlineLogo");

        return app;
    }

    private static async Task<IResult> GetAll(IAirlineService airlines, CancellationToken cancellationToken)
    {
        var list = await airlines.GetAllForAdminAsync(cancellationToken);
        return TypedResults.Ok(list.Select(a => a.ToAdminListItem()));
    }

    private static async Task<IResult> Create(AirlineCreateDto dto, IAirlineService airlines, CancellationToken cancellationToken)
    {
        try
        {
            var airline = await airlines.CreateAsync(dto.Code, dto.Name, dto.Country, dto.LogoFileId, cancellationToken);
            return TypedResults.Created($"/api/v1/admin/airlines/{airline.Id}", airline.ToAdminListItem());
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> Update(Guid id, AirlineUpdateDto dto, IAirlineService airlines, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await airlines.UpdateAsync(id, dto.Code, dto.Name, dto.Country, dto.LogoFileId, dto.IsActive, cancellationToken);
            return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToAdminListItem());
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ToggleStatus(Guid id, AirlineUpdateDto dto, IAirlineService airlines, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await airlines.UpdateAsync(id, null, null, null, null, dto.IsActive, cancellationToken);
            return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToAdminListItem());
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> Delete(Guid id, IAirlineService airlines, CancellationToken cancellationToken)
    {
        var existing = await airlines.GetByIdForAdminAsync(id, cancellationToken);
        if (existing is null) return TypedResults.NotFound();
        if (!existing.IsActive && existing.DeletedAt.HasValue)
            return TypedResults.Conflict(new { error = "Airline is already deactivated." });

        var deleted = await airlines.DeleteAsync(id, cancellationToken);
        return deleted
            ? TypedResults.Ok(new { message = "Airline deactivated successfully." })
            : TypedResults.NotFound();
    }

    private static async Task<IResult> Restore(Guid id, IAirlineService airlines, CancellationToken cancellationToken)
    {
        var existing = await airlines.GetByIdForAdminAsync(id, cancellationToken);
        if (existing is null) return TypedResults.NotFound();
        if (existing.IsActive && existing.DeletedAt is null)
            return TypedResults.Conflict(new { error = "Airline is already active." });

        var restored = await airlines.RestoreAsync(id, cancellationToken);
        return restored is null ? TypedResults.NotFound() : TypedResults.Ok(restored.ToAdminListItem());
    }

    private static async Task<IResult> UploadLogo(
        Guid id,
        IFormFile file,
        HttpRequest request,
        IWebHostEnvironment env,
        IAirlineService airlines,
        AppDbContext db,
        ICurrentUserAccessor currentUser,
        CancellationToken cancellationToken)
    {
        var userId = await currentUser.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        var airline = await airlines.GetByIdForAdminAsync(id, cancellationToken);
        if (airline is null) return TypedResults.NotFound();

        if (!file.ContentType.StartsWith("image/"))
            return TypedResults.BadRequest(new { error = "Only image files are allowed." });

        if (file.Length > 5 * 1024 * 1024)
            return TypedResults.BadRequest(new { error = "File size must not exceed 5 MB." });

        // Remove old logo file if it was a locally uploaded file
        if (airline.LogoFileId.HasValue)
        {
            var oldFile = await db.UploadedFiles.FindAsync([airline.LogoFileId.Value], cancellationToken);
            if (oldFile is not null)
            {
                var localPath = ExtractLocalUploadPath(oldFile.StoragePath, env.ContentRootPath);
                if (localPath is not null && File.Exists(localPath)) File.Delete(localPath);
                db.UploadedFiles.Remove(oldFile);
            }
        }

        // Save the new file using the absolute path from the environment
        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var uploadsDir = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsDir);
        var diskPath = Path.Combine(uploadsDir, fileName);

        await using (var stream = new FileStream(diskPath, FileMode.Create))
            await file.CopyToAsync(stream, cancellationToken);

        // Build absolute URL so the frontend can load it directly from the API host
        var apiBase = $"{request.Scheme}://{request.Host}";
        var storagePath = $"{apiBase}/uploads/{fileName}";

        // Create the UploadedFile record
        var uploadedFile = new UploadedFile
        {
            UploadedByUserId = userId.Value,
            FileName = fileName,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            StoragePath = storagePath,
            RelatedEntityName = "Airline",
            RelatedEntityId = id,
        };
        db.UploadedFiles.Add(uploadedFile);
        await db.SaveChangesAsync(cancellationToken);

        var updated = await airlines.UpdateAsync(id, null, null, null, uploadedFile.Id, null, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToAdminListItem());
    }

    private static async Task<IResult> DeleteLogo(
        Guid id,
        IWebHostEnvironment env,
        IAirlineService airlines,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var airline = await airlines.GetByIdForAdminAsync(id, cancellationToken);
        if (airline is null) return TypedResults.NotFound();
        if (airline.LogoFileId is null) return TypedResults.NotFound();

        var logoFile = await db.UploadedFiles.FindAsync([airline.LogoFileId.Value], cancellationToken);
        if (logoFile is not null)
        {
            var localPath = ExtractLocalUploadPath(logoFile.StoragePath, env.ContentRootPath);
            if (localPath is not null && File.Exists(localPath)) File.Delete(localPath);
            db.UploadedFiles.Remove(logoFile);
        }

        // Set LogoFileId to null on the airline
        var airlineEntity = await db.Airlines.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (airlineEntity is not null)
        {
            airlineEntity.LogoFileId = null;
            airlineEntity.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        var updated = await airlines.GetByIdForAdminAsync(id, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToAdminListItem());
    }

    // Returns the absolute disk path for a locally uploaded file, or null for external URLs.
    private static string? ExtractLocalUploadPath(string storagePath, string contentRootPath)
    {
        // Stored as absolute URL: http://localhost:5194/uploads/filename.ext
        var uploadsSegment = "/uploads/";
        var idx = storagePath.IndexOf(uploadsSegment, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var fileName = storagePath[(idx + uploadsSegment.Length)..]; // "filename.ext"
        return Path.Combine(contentRootPath, "wwwroot", "uploads", fileName);
    }
}
