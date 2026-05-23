using FlightKS.Data;
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
}
