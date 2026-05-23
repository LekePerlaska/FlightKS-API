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
        group.MapPatch("/{notificationId:guid}", Update).WithName("UpdateNotification");

        return app;
    }

    private static async Task<IResult> GetAll(ICurrentUserAccessor current, INotificationService notifications, CancellationToken cancellationToken, bool? unreadOnly = null)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        var list = await notifications.GetForUserAsync(userId.Value, unreadOnly, cancellationToken);
        return TypedResults.Ok(list.Select(n => n.ToDto()));
    }

    private static async Task<IResult> Update(Guid notificationId, NotificationUpdateDto dto, ICurrentUserAccessor current, INotificationService notifications, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.Unauthorized();

        var updated = await notifications.MarkReadAsync(notificationId, userId.Value, dto.IsRead, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToDto());
    }
}
