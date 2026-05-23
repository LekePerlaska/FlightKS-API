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
            .AnyAsync(u => u.KeycloakUserId == keycloakUserId || u.Email == email, cancellationToken);
        if (exists)
            throw new InvalidOperationException("A user with this Keycloak id or email already exists.");

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
        await AssignDefaultRoleAsync(user, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Users.AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public Task<User?> GetByKeycloakIdAsync(string keycloakUserId, CancellationToken cancellationToken = default) =>
        db.Users.AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
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
        return user;
    }

    private async Task AssignDefaultRoleAsync(User user, CancellationToken cancellationToken)
    {
        var userRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "User", cancellationToken);
        if (userRole is null) return;
        user.UserRoles.Add(new UserRole { User = user, RoleId = userRole.Id });
    }
}
