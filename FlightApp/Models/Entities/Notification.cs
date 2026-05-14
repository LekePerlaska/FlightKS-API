using FlightKS.Enums;

namespace FlightKS.Models.Entities;

public class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public NotifType Type { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public bool Read { get; set; }
    public Guid? ReferenceId { get; set; }              // polymorphic: bookings or price_alerts
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
