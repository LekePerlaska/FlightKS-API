using FlightKS.Enums;

namespace FlightKS.Models.Dtos.Bookings;

public class BookingCreateDto
{
    public required string FlightId { get; set; }
    public CabinClass CabinClass { get; set; }
    public TripType TripType { get; set; } = TripType.OneWay;
    public DateOnly? ReturnDate { get; set; }
    public required List<BookingPassengerCreateDto> Passengers { get; set; }
    public required PaymentSummaryDto PaymentSummary { get; set; }
}
