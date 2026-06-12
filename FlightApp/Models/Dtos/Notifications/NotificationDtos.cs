namespace FlightKS.Models.Dtos.Notifications;

public record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    string Type,
    bool IsRead,
    string? RelatedEntityName,
    Guid? RelatedEntityId,
    DateTime CreatedAt);

public record NotificationUpdateDto(bool IsRead);
