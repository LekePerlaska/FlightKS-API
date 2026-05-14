using FlightKS.Data;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.Users;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class UserService(AppDbContext db, IPasswordHasher passwordHasher) : IUserService
{
    public async Task<IEnumerable<UserResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await db.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return users.Select(UserMapping.ToResponse);
    }

    public async Task<UserResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await FindByIdAsync(id, tracked: false, cancellationToken);
        return user?.ToResponse();
    }

    public async Task<UserResponseDto> CreateAsync(UserCreateDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureEmailIsAvailableAsync(dto.Email, cancellationToken);

        var passwordHash = passwordHasher.Hash(dto.Password);
        var user = dto.ToEntity(passwordHash);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return user.ToResponse();
    }

    public async Task<UserResponseDto?> UpdateAsync(Guid id, UserUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var user = await FindByIdAsync(id, tracked: true, cancellationToken);
        if (user is null) return null;

        dto.ApplyTo(user);
        await db.SaveChangesAsync(cancellationToken);

        return user.ToResponse();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await FindByIdAsync(id, tracked: true, cancellationToken);
        if (user is null) return false;

        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    private Task<User?> FindByIdAsync(Guid id, bool tracked, CancellationToken cancellationToken)
    {
        var query = tracked ? db.Users : db.Users.AsNoTracking();
        return query.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    private async Task EnsureEmailIsAvailableAsync(string email, CancellationToken cancellationToken)
    {
        var emailIsTaken = await db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (emailIsTaken)
            throw new InvalidOperationException($"Email '{email}' is already in use.");
    }
}
