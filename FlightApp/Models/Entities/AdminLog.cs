namespace FlightKS.Models.Entities;

public class AdminLog
{
    public Guid Id { get; set; }

    public Guid AdminUserId { get; set; }
    public User AdminUser { get; set; } = null!;

    public required string Action { get; set; }
    public required string EntityName { get; set; }
    public Guid? EntityId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
