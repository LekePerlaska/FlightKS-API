using FlightKS.Auth;
using FlightKS.Data;
using FlightKS.Endpoints;
using FlightKS.Exceptions;
using FlightKS.Mappers;
using FlightKS.Middleware;
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
        group.MapPost("/", Create).WithName("AdminCreateAirline").WithValidation<AirlineCreateDto>();
        group.MapPut("/{id:guid}", Update).WithName("AdminUpdateAirline").WithValidation<AirlineUpdateDto>();
        group.MapPatch("/{id:guid}", ToggleStatus).WithName("AdminToggleAirlineStatus").WithValidation<AirlineUpdateDto>();
        group.MapDelete("/{id:guid}", Delete).WithName("AdminDeleteAirline");
        group.MapPatch("/{id:guid}/restore", Restore).WithName("AdminRestoreAirline");
        group.MapPost("/{id:guid}/logo", UploadLogo).WithName("AdminUploadAirlineLogo").DisableAntiforgery()
            .AddEndpointFilter<RequireCurrentUserFilter>();
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
        var airline = await airlines.CreateAsync(dto.Code, dto.Name, dto.Country, dto.LogoFileId, cancellationToken);
        return TypedResults.Created($"/api/v1/admin/airlines/{airline.Id}", airline.ToAdminListItem());
    }

    private static async Task<IResult> Update(Guid id, AirlineUpdateDto dto, IAirlineService airlines, CancellationToken cancellationToken)
    {
        var updated = await airlines.UpdateAsync(id, dto.Code, dto.Name, dto.Country, dto.LogoFileId, dto.IsActive, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToAdminListItem());
    }

    private static async Task<IResult> ToggleStatus(Guid id, AirlineUpdateDto dto, IAirlineService airlines, CancellationToken cancellationToken)
    {
        var updated = await airlines.UpdateAsync(id, null, null, null, null, dto.IsActive, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToAdminListItem());
    }

    private static async Task<IResult> Delete(Guid id, IAirlineService airlines, CancellationToken cancellationToken)
    {
        var existing = await airlines.GetByIdForAdminAsync(id, cancellationToken);
        if (existing is null) return TypedResults.NotFound();
        if (!existing.IsActive && existing.DeletedAt.HasValue)
            throw new BusinessRuleException("Airline is already deactivated.");

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
            throw new BusinessRuleException("Airline is already active.");

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
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var airline = await airlines.GetByIdForAdminAsync(id, cancellationToken);
        if (airline is null) return TypedResults.NotFound();

        if (file.Length > 5 * 1024 * 1024)
            throw new ValidationException("file", "File size must not exceed 5 MB.");
        if (!await IsAllowedImageAsync(file))
            throw new ValidationException("file", "Only JPEG, PNG, GIF, or WebP images are allowed.");

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

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var uploadsDir = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsDir);
        var diskPath = Path.Combine(uploadsDir, fileName);

        await using (var stream = new FileStream(diskPath, FileMode.Create))
            await file.CopyToAsync(stream, cancellationToken);

        var apiBase = $"{request.Scheme}://{request.Host}";
        var storagePath = $"{apiBase}/uploads/{fileName}";

        var uploadedFile = new UploadedFile
        {
            UploadedByUserId = httpContext.CurrentUserId(),
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

    private static async Task<bool> IsAllowedImageAsync(IFormFile file)
    {
        var buffer = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
        if (read < 4) return false;

        // JPEG: FF D8 FF
        if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF) return true;
        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47) return true;
        // GIF: 47 49 46 38
        if (buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x38) return true;
        // WebP: RIFF????WEBP (bytes 0-3 = RIFF, bytes 8-11 = WEBP)
        if (read >= 12 &&
            buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46 &&
            buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50) return true;

        return false;
    }

    private static string? ExtractLocalUploadPath(string storagePath, string contentRootPath)
    {
        var uploadsSegment = "/uploads/";
        var idx = storagePath.IndexOf(uploadsSegment, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var fileName = storagePath[(idx + uploadsSegment.Length)..];
        return Path.Combine(contentRootPath, "wwwroot", "uploads", fileName);
    }
}
