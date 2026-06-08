using FlightKS.Enums;

namespace FlightKS.Models.Entities;

public class Seat
{
    public Guid Id { get; set; }

    public Guid AircraftId { get; set; }
    public Aircraft Aircraft { get; set; } = null!;

    public required string SeatNumber { get; set; }
    public SeatClass SeatClass { get; set; } = SeatClass.Economy;
    public bool IsWindow { get; set; }
    public bool IsAisle { get; set; }
    public bool ExtraLegroom { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<FlightSeat> FlightSeats { get; set; } = [];
}
