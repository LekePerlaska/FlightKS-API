using FlightKS.Auth;
using FlightKS.Endpoints;
using FlightKS.Mappers;
using FlightKS.Middleware;
using FlightKS.Models.Dtos;
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

    private static async Task<IResult> GetAll(
        HttpContext httpContext,
        INotificationService notifications,
        CancellationToken cancellationToken,
        bool? unreadOnly = null,
        int page = 1,
        int pageSize = 50)
    {
        var (items, total) = await notifications.GetForUserAsync(httpContext.CurrentUserId(), unreadOnly, page, pageSize, cancellationToken);
        return TypedResults.Ok(new PagedResult<NotificationDto>(items.Select(n => n.ToDto()).ToList(), total, page, pageSize));
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
