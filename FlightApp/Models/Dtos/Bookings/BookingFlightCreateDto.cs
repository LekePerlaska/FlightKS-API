namespace FlightKS.Models.Dtos.Bookings;

public class BookingFlightCreateDto
{
    public Guid FlightId { get; set; }
    public int LegOrder { get; set; }
}
