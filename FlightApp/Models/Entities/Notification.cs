namespace FlightKS.Models.Entities;

public class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public required string Title { get; set; }
    public required string Message { get; set; }
    public required string Type { get; set; }
    public bool IsRead { get; set; }
    public string? RelatedEntityName { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
