using FlightKS.Enums;

namespace FlightKS.Models.Entities;

public class FlightSchedule
{
    public Guid Id { get; set; }

    public Guid FlightId { get; set; }
    public Flight Flight { get; set; } = null!;

    public Guid AircraftId { get; set; }
    public Aircraft Aircraft { get; set; } = null!;

    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public FlightScheduleStatus Status { get; set; } = FlightScheduleStatus.Scheduled;
    public int AvailableSeats { get; set; }
    public decimal CurrentPrice { get; set; }
    public string? Gate { get; set; }
    public string? DelayReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<FlightSeat> FlightSeats { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = [];
    public ICollection<ItinerarySegment> ItinerarySegments { get; set; } = [];
}
