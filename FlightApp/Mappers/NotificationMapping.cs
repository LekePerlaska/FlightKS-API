using FlightKS.Models.Dtos.Notifications;
using FlightKS.Models.Entities;

namespace FlightKS.Mappers;

public static class NotificationMapping
{
    public static NotificationDto ToDto(this Notification n) => new(
        n.Id, n.Title, n.Message, n.Type, n.IsRead,
        n.RelatedEntityName, n.RelatedEntityId, n.CreatedAt);
}
