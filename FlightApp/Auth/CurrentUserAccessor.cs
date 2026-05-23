using System.Security.Claims;
using FlightKS.Data;
using FlightKS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Auth;

public class CurrentUserAccessor(IHttpContextAccessor httpContext, AppDbContext db) : ICurrentUserAccessor
{
    public string? KeycloakUserId =>
        httpContext.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.HttpContext?.User.FindFirstValue("sub");

    public bool IsAuthenticated => httpContext.HttpContext?.User.Identity?.IsAuthenticated == true;

    public async Task<User?> GetUserAsync(CancellationToken cancellationToken = default)
    {
        var kid = KeycloakUserId;
        if (string.IsNullOrEmpty(kid)) return null;
        return await db.Users.AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.KeycloakUserId == kid, cancellationToken);
    }

    public async Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken = default)
    {
        var kid = KeycloakUserId;
        if (string.IsNullOrEmpty(kid)) return null;
        var id = await db.Users.AsNoTracking()
            .Where(u => u.KeycloakUserId == kid)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return id;
    }
}
