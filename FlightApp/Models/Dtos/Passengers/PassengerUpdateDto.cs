using FlightKS.Enums;

namespace FlightKS.Models.Dtos.Passengers;

public class PassengerUpdateDto
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public PassengerType Type { get; set; }
    public string? PassportNumber { get; set; }
    public string? Nationality { get; set; }
}
