namespace FlightKS.Models.Entities;

public class ItinerarySegment
{
    public Guid Id { get; set; }

    public Guid ItineraryId { get; set; }
    public Itinerary Itinerary { get; set; } = null!;

    public Guid FlightScheduleId { get; set; }
    public FlightSchedule FlightSchedule { get; set; } = null!;

    public int SegmentOrder { get; set; }
    public int? LayoverMinutesAfterSegment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
