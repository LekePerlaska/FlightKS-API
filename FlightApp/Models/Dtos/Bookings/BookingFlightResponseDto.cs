using FlightKS.Models.Dtos.Flights;

namespace FlightKS.Models.Dtos.Bookings;

public class BookingFlightResponseDto
{
    public int LegOrder { get; set; }
    public required FlightResponseDto Flight { get; set; }
}
