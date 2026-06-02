using FlightKS.Auth;
using FlightKS.Data;
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
        group.MapPost("/", Create).WithName("AdminCreateAirline");
        group.MapPut("/{id:guid}", Update).WithName("AdminUpdateAirline");
        group.MapPatch("/{id:guid}", ToggleStatus).WithName("AdminToggleAirlineStatus");
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

        if (!file.ContentType.StartsWith("image/"))
            throw new ValidationException("file", "Only image files are allowed.");
        if (file.Length > 5 * 1024 * 1024)
            throw new ValidationException("file", "File size must not exceed 5 MB.");

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

    private static string? ExtractLocalUploadPath(string storagePath, string contentRootPath)
    {
        var uploadsSegment = "/uploads/";
        var idx = storagePath.IndexOf(uploadsSegment, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var fileName = storagePath[(idx + uploadsSegment.Length)..];
        return Path.Combine(contentRootPath, "wwwroot", "uploads", fileName);
    }
}
