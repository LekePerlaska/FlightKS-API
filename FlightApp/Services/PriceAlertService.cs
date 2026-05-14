using FlightKS.Data;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.Alerts;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class PriceAlertService(AppDbContext db) : IPriceAlertService
{
    public async Task<IEnumerable<PriceAlertResponseDto>> GetAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var alerts = await db.PriceAlerts
            .AsNoTracking()
            .Include(a => a.OriginAirport)
            .Include(a => a.DestinationAirport)
            .Where(a => a.UserId == userId && a.Active)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return alerts.Select(PriceAlertMapping.ToResponse);
    }

    public async Task<PriceAlertResponseDto> CreateAsync(Guid userId, PriceAlertCreateDto dto, CancellationToken cancellationToken = default)
    {
        var alert = dto.ToEntity(userId);
        db.PriceAlerts.Add(alert);
        await db.SaveChangesAsync(cancellationToken);

        await db.Entry(alert).Reference(a => a.OriginAirport).LoadAsync(cancellationToken);
        await db.Entry(alert).Reference(a => a.DestinationAirport).LoadAsync(cancellationToken);

        return alert.ToResponse();
    }

    public async Task<bool> DeactivateAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var alert = await db.PriceAlerts.FirstOrDefaultAsync(a => a.UserId == userId && a.Id == id, cancellationToken);
        if (alert is null) return false;

        alert.Active = false;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
