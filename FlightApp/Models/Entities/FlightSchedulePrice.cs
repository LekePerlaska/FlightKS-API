using FlightKS.Enums;

namespace FlightKS.Models.Entities;

public class FlightSchedulePrice
{
    public Guid Id { get; set; }

    public Guid FlightScheduleId { get; set; }
    public FlightSchedule FlightSchedule { get; set; } = null!;

    public SeatClass SeatClass { get; set; }
    public decimal Price { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
