using FlightKS.Data;
using FlightKS.Hubs;
using FlightKS.Mappers;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class NotificationService(
    AppDbContext db,
    IHubContext<NotificationHub> hub,
    IEmailSender emailSender,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task<Notification> CreateAsync(
        Guid userId, string title, string message, string type,
        string? relatedEntityName = null, Guid? relatedEntityId = null,
        bool sendEmail = false, string? emailSubject = null, string? emailHtml = null,
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

        // Use CancellationToken.None so a client disconnect after SaveChangesAsync does not
        // abort the push and surface a false 500 to the caller (the DB row is already committed).
        await hub.Clients.Group($"user:{userId}")
            .SendAsync(NotificationHub.NotificationReceived,
                new { notification = notification.ToDto(), unreadCount },
                CancellationToken.None);

        if (sendEmail && !string.IsNullOrEmpty(emailSubject) && !string.IsNullOrEmpty(emailHtml))
        {
            var user = await db.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.Email, u.FullName })
                .FirstOrDefaultAsync(cancellationToken);

            if (user is not null && !string.IsNullOrEmpty(user.Email))
                _ = emailSender.SendAsync(user.Email, user.FullName, emailSubject, emailHtml, CancellationToken.None)
                    .ContinueWith(
                        t => logger.LogError(t.Exception, "Failed to send email '{Subject}' to {Email}", emailSubject, user.Email),
                        CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted,
                        TaskScheduler.Default);
        }

        return notification;
    }

    public async Task CreateBulkAsync(
        IReadOnlyList<Guid> userIds, string title, string message, string type,
        string? relatedEntityName = null, Guid? relatedEntityId = null,
        bool sendEmail = false, string? emailSubject = null, string? emailHtml = null,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0) return;

        var now = DateTime.UtcNow;
        var notifications = userIds.Select(uid => new Notification
        {
            UserId = uid,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            RelatedEntityName = relatedEntityName,
            RelatedEntityId = relatedEntityId,
            CreatedAt = now,
        }).ToList();

        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync(cancellationToken);

        // Fetch all unread counts in one GROUP BY query.
        var unreadCounts = await db.Notifications
            .Where(n => userIds.Contains(n.UserId) && !n.IsRead)
            .GroupBy(n => n.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        // Push all hub events in parallel.
        await Task.WhenAll(notifications.Select(n =>
            hub.Clients.Group($"user:{n.UserId}")
                .SendAsync(NotificationHub.NotificationReceived,
                    new { notification = n.ToDto(), unreadCount = unreadCounts.GetValueOrDefault(n.UserId) },
                    CancellationToken.None)));

        if (sendEmail && !string.IsNullOrEmpty(emailSubject) && !string.IsNullOrEmpty(emailHtml))
        {
            var users = await db.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Email, u.FullName })
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            foreach (var n in notifications)
            {
                if (!users.TryGetValue(n.UserId, out var user) || string.IsNullOrEmpty(user.Email))
                    continue;
                _ = emailSender.SendAsync(user.Email, user.FullName, emailSubject, emailHtml, CancellationToken.None)
                    .ContinueWith(
                        t => logger.LogError(t.Exception, "Failed to send email '{Subject}' to {Email}", emailSubject, user.Email),
                        CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted,
                        TaskScheduler.Default);
            }
        }
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
