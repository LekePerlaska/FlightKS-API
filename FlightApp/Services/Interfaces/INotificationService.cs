using FlightKS.Models.Entities;

namespace FlightKS.Services.Interfaces;

public interface INotificationService
{
    Task<Notification> CreateAsync(
        Guid userId, string title, string message, string type,
        string? relatedEntityName = null, Guid? relatedEntityId = null,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Notification> Items, int Total)> GetForUserAsync(
        Guid userId, bool? unreadOnly, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
    Task<Notification?> MarkReadAsync(Guid notificationId, Guid userId, bool isRead = true, CancellationToken cancellationToken = default);
    Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
}
