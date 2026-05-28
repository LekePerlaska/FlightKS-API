using FlightKS.Data;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class UserService(AppDbContext db) : IUserService
{
    public async Task<User> CreateAsync(
        string keycloakUserId,
        string fullName,
        string email,
        string? phoneNumber = null,
        DateOnly? dateOfBirth = null,
        string? passportNumber = null,
        string? nationality = null,
        CancellationToken cancellationToken = default)
    {
        var exists = await db.Users.AsNoTracking()
            .AnyAsync(u => u.Email == email || u.KeycloakUserId == keycloakUserId, cancellationToken);

        if (exists)
            throw new InvalidOperationException("A user with this email already exists.");

        var user = new User
        {
            KeycloakUserId = keycloakUserId,
            FullName = fullName,
            Email = email,
            PhoneNumber = phoneNumber,
            DateOfBirth = dateOfBirth,
            PassportNumber = passportNumber,
            Nationality = nationality,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return await db.Users.AsNoTracking()
            .Include(u => u.UploadedFiles)
            .FirstAsync(u => u.Id == user.Id, cancellationToken);
    }

    public async Task<User> GetOrCreateAsync(
        string keycloakUserId,
        string email,
        string fullName,
        CancellationToken cancellationToken = default)
    {
        var existing = await db.Users.AsNoTracking()
            .Include(u => u.UploadedFiles)
            .FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId, cancellationToken);

        if (existing is not null) return existing;

        var user = new User
        {
            KeycloakUserId = keycloakUserId,
            FullName = string.IsNullOrWhiteSpace(fullName) ? email : fullName,
            Email = email,
        };

        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return await db.Users.AsNoTracking()
                .Include(u => u.UploadedFiles)
                .FirstAsync(u => u.KeycloakUserId == keycloakUserId, cancellationToken);
        }

        return await db.Users.AsNoTracking()
            .Include(u => u.UploadedFiles)
            .FirstAsync(u => u.Id == user.Id, cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Users.AsNoTracking()
            .Include(u => u.UploadedFiles)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public Task<User?> GetByKeycloakIdAsync(string keycloakUserId, CancellationToken cancellationToken = default) =>
        db.Users.AsNoTracking()
            .Include(u => u.UploadedFiles)
            .FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId, cancellationToken);

    public async Task<User?> UpdateAsync(
        Guid userId,
        string? fullName = null,
        string? phoneNumber = null,
        DateOnly? dateOfBirth = null,
        string? passportNumber = null,
        string? nationality = null,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return null;

        if (fullName is not null) user.FullName = fullName;
        if (phoneNumber is not null) user.PhoneNumber = phoneNumber;
        if (dateOfBirth is not null) user.DateOfBirth = dateOfBirth;
        if (passportNumber is not null) user.PassportNumber = passportNumber;
        if (nationality is not null) user.Nationality = nationality;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return await db.Users.AsNoTracking()
            .Include(u => u.UploadedFiles)
            .FirstAsync(u => u.Id == user.Id, cancellationToken);
    }
}
