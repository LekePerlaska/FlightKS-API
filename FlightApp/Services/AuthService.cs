using FlightKS.Data;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.Auth;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class AuthService(AppDbContext db, IPasswordHasher passwordHasher) : IAuthService
{
    public async Task<AuthResponseDto> SignUpAsync(SignUpRequestDto dto, CancellationToken cancellationToken = default)
    {
        var exists = await db.Users.AsNoTracking().AnyAsync(u => u.Email == dto.Email, cancellationToken);
        if (exists)
            throw new InvalidOperationException($"Email '{dto.Email}' is already in use.");

        var user = dto.ToEntity(passwordHasher.Hash(dto.Password));
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            User = user.ToResponse(),
            Token = IssueToken(user.Id),
        };
    }

    public async Task<AuthResponseDto?> SignInAsync(SignInRequestDto dto, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .Include(u => u.Passport)
            .Include(u => u.Preferences)
            .Include(u => u.Loyalty)
            .FirstOrDefaultAsync(u => u.Email == dto.Email, cancellationToken);

        if (user is null || !passwordHasher.Verify(dto.Password, user.PasswordHash))
            return null;

        return new AuthResponseDto
        {
            User = user.ToResponse(),
            Token = IssueToken(user.Id),
        };
    }

    // TODO: replace with JWT signing once auth middleware is wired up.
    private static string IssueToken(Guid userId) =>
        Convert.ToBase64String(userId.ToByteArray());
}
