using FlightKS.Enums;

namespace FlightKS.Models.Dtos.Passengers;

public class PassengerResponseDto
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public PassengerType Type { get; set; }
    public string? PassportNumber { get; set; }
    public string? Nationality { get; set; }
}
