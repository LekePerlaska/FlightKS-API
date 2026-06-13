using FlightKS.Data;
using FlightKS.Hubs;
using FlightKS.Mappers;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class NotificationService(AppDbContext db, IHubContext<NotificationHub> hub) : INotificationService
{
    public async Task<Notification> CreateAsync(
        Guid userId, string title, string message, string type,
        string? relatedEntityName = null, Guid? relatedEntityId = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            RelatedEntityName = relatedEntityName,
            RelatedEntityId = relatedEntityId,
            CreatedAt = DateTime.UtcNow,
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(cancellationToken);

        var unreadCount = await db.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

        await hub.Clients.Group($"user:{userId}")
            .SendAsync(NotificationHub.NotificationReceived,
                new { notification = notification.ToDto(), unreadCount },
                cancellationToken);

        return notification;
    }

    public async Task<(IReadOnlyList<Notification> Items, int Total)> GetForUserAsync(
        Guid userId, bool? unreadOnly, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var q = db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        if (unreadOnly == true) q = q.Where(n => !n.IsRead);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<Notification?> GetByIdAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default) =>
        db.Notifications.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);

    public async Task<Notification?> MarkReadAsync(Guid notificationId, Guid userId, bool isRead = true, CancellationToken cancellationToken = default)
    {
        var notif = await db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);
        if (notif is null) return null;

        notif.IsRead = isRead;
        await db.SaveChangesAsync(cancellationToken);
        return notif;
    }

    public async Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true), cancellationToken);

    public async Task<bool> DeleteAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var deleted = await db.Notifications
            .Where(n => n.Id == notificationId && n.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }
}
