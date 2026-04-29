namespace FlightKS.Models.Entities;

public class Passenger
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public PassengerType Type { get; set; }
    public string? PassportNumber { get; set; }
    public string? Nationality { get; set; }

    public ICollection<PassengerSegment> Segments { get; set; } = [];
}
