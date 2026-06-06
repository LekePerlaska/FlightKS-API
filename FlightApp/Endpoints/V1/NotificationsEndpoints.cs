using FlightKS.Auth;
using FlightKS.Endpoints;
using FlightKS.Mappers;
using FlightKS.Middleware;
using FlightKS.Models.Dtos.Notifications;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/notifications").WithTags("Notifications")
            .RequireAuthorization()
            .RequireCurrentUser();

        group.MapGet("/", GetAll).WithName("GetNotifications");
        group.MapGet("/{notificationId:guid}", GetById).WithName("GetNotificationById");
        group.MapPatch("/{notificationId:guid}", Update).WithName("UpdateNotification").WithValidation<NotificationUpdateDto>();
        group.MapPatch("/", MarkAllRead).WithName("MarkAllNotificationsRead");
        group.MapDelete("/{notificationId:guid}", Delete).WithName("DeleteNotification");

        return app;
    }

    private static async Task<IResult> GetAll(HttpContext httpContext, INotificationService notifications, CancellationToken cancellationToken, bool? unreadOnly = null)
    {
        var list = await notifications.GetForUserAsync(httpContext.CurrentUserId(), unreadOnly, cancellationToken);
        return TypedResults.Ok(list.Select(n => n.ToDto()));
    }

    private static async Task<IResult> GetById(Guid notificationId, HttpContext httpContext, INotificationService notifications, CancellationToken cancellationToken)
    {
        var notif = await notifications.GetByIdAsync(notificationId, httpContext.CurrentUserId(), cancellationToken);
        return notif is null ? TypedResults.NotFound() : TypedResults.Ok(notif.ToDto());
    }

    private static async Task<IResult> Update(Guid notificationId, NotificationUpdateDto dto, HttpContext httpContext, INotificationService notifications, CancellationToken cancellationToken)
    {
        var updated = await notifications.MarkReadAsync(notificationId, httpContext.CurrentUserId(), dto.IsRead, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToDto());
    }

    private static async Task<IResult> MarkAllRead(HttpContext httpContext, INotificationService notifications, CancellationToken cancellationToken)
    {
        var count = await notifications.MarkAllReadAsync(httpContext.CurrentUserId(), cancellationToken);
        return TypedResults.Ok(new { updatedCount = count });
    }

    private static async Task<IResult> Delete(Guid notificationId, HttpContext httpContext, INotificationService notifications, CancellationToken cancellationToken)
    {
        var deleted = await notifications.DeleteAsync(notificationId, httpContext.CurrentUserId(), cancellationToken);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
