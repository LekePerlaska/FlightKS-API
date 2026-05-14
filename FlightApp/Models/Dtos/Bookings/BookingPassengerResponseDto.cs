using FlightKS.Enums;

namespace FlightKS.Models.Dtos.Bookings;

public class BookingPassengerResponseDto
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public required string Nationality { get; set; }
    public required string PassportNumber { get; set; }
    public DateOnly PassportExpiry { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? SeatNumber { get; set; }
    public string? SpecialRequests { get; set; }
    public string? FrequentFlyerNumber { get; set; }
    public decimal Fare { get; set; }
    public PassengerType PassengerType { get; set; }
}
