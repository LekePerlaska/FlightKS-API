using FlightKS.Data;
using FlightKS.Exceptions;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class BaggageOptionService(AppDbContext db) : IBaggageOptionService
{
    public async Task<IEnumerable<BaggageOption>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.BaggageOptions.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Price)
            .ToListAsync(cancellationToken);

    public Task<BaggageOption?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.BaggageOptions.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IEnumerable<BaggageOption>> GetAllForAdminAsync(CancellationToken cancellationToken = default) =>
        await db.BaggageOptions.IgnoreQueryFilters().AsNoTracking()
            .OrderBy(b => b.Price)
            .ToListAsync(cancellationToken);

    public Task<BaggageOption?> GetByIdForAdminAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.BaggageOptions.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<BaggageOption> CreateAsync(string name, decimal weightKg, decimal price, string? description, CancellationToken cancellationToken = default)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("name", "Name is required.");
        if (weightKg < 0)
            throw new ValidationException("weightKg", "Weight cannot be negative.");
        if (price < 0)
            throw new ValidationException("price", "Price cannot be negative.");

        var option = new BaggageOption
        {
            Name = name,
            WeightKg = weightKg,
            Price = price,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
        };
        db.BaggageOptions.Add(option);
        await db.SaveChangesAsync(cancellationToken);
        return option;
    }

    public async Task<BaggageOption?> UpdateAsync(Guid id, string? name, decimal? weightKg, decimal? price, string? description, bool? isActive, CancellationToken cancellationToken = default)
    {
        var option = await db.BaggageOptions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (option is null) return null;

        if (weightKg is < 0)
            throw new ValidationException("weightKg", "Weight cannot be negative.");
        if (price is < 0)
            throw new ValidationException("price", "Price cannot be negative.");

        if (name is not null)
        {
            var trimmed = name.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                throw new ValidationException("name", "Name is required.");
            option.Name = trimmed;
        }
        if (weightKg is not null) option.WeightKg = weightKg.Value;
        if (price is not null) option.Price = price.Value;
        if (description is not null)
            option.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (isActive is not null) option.IsActive = isActive.Value;
        option.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return await db.BaggageOptions.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var option = await db.BaggageOptions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (option is null) return false;

        var now = DateTime.UtcNow;
        option.IsActive = false;
        option.DeletedAt = now;
        option.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<BaggageOption?> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var option = await db.BaggageOptions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (option is null) return null;

        option.IsActive = true;
        option.DeletedAt = null;
        option.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return await db.BaggageOptions.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }
}
