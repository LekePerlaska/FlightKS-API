using FlightKS.Auth;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.Notifications;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/notifications").WithTags("Notifications").RequireAuthorization();

        group.MapGet("/", GetAll).WithName("GetNotifications");
        group.MapGet("/{notificationId:guid}", GetById).WithName("GetNotificationById");
        group.MapPatch("/{notificationId:guid}", Update).WithName("UpdateNotification");
        group.MapPatch("/", MarkAllRead).WithName("MarkAllNotificationsRead");
        group.MapDelete("/{notificationId:guid}", Delete).WithName("DeleteNotification");

        return app;
    }

    private static async Task<IResult> GetAll(ICurrentUserAccessor current, INotificationService notifications, CancellationToken cancellationToken, bool? unreadOnly = null)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        var list = await notifications.GetForUserAsync(userId.Value, unreadOnly, cancellationToken);
        return TypedResults.Ok(list.Select(n => n.ToDto()));
    }

    private static async Task<IResult> GetById(Guid notificationId, ICurrentUserAccessor current, INotificationService notifications, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        var notif = await notifications.GetByIdAsync(notificationId, userId.Value, cancellationToken);
        return notif is null ? TypedResults.NotFound() : TypedResults.Ok(notif.ToDto());
    }

    private static async Task<IResult> Update(Guid notificationId, NotificationUpdateDto dto, ICurrentUserAccessor current, INotificationService notifications, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        var updated = await notifications.MarkReadAsync(notificationId, userId.Value, dto.IsRead, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToDto());
    }

    private static async Task<IResult> MarkAllRead(ICurrentUserAccessor current, INotificationService notifications, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        var count = await notifications.MarkAllReadAsync(userId.Value, cancellationToken);
        return TypedResults.Ok(new { updatedCount = count });
    }

    private static async Task<IResult> Delete(Guid notificationId, ICurrentUserAccessor current, INotificationService notifications, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        var deleted = await notifications.DeleteAsync(notificationId, userId.Value, cancellationToken);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
