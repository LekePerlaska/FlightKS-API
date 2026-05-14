using FlightKS.Models.Dtos.Passengers;

namespace FlightKS.Models.Dtos.Bookings;

public class BookingCreateDto
{
    public Guid UserId { get; set; }
    public required string Currency { get; set; }
    public required List<BookingFlightCreateDto> Flights { get; set; }
    public required List<PassengerCreateDto> Passengers { get; set; }
}
