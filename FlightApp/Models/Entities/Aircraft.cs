namespace FlightKS.Models.Entities;

public class Aircraft
{
    public Guid Id { get; set; }

    public Guid AirlineId { get; set; }
    public Airline Airline { get; set; } = null!;

    public required string Model { get; set; }
    public required string RegistrationNumber { get; set; }
    public int TotalSeats { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<Seat> Seats { get; set; } = [];
    public ICollection<FlightSchedule> FlightSchedules { get; set; } = [];
}
