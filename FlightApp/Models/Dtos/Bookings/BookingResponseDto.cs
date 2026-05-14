using FlightKS.Enums;
using FlightKS.Models.Dtos.Flights;

namespace FlightKS.Models.Dtos.Bookings;

public class BookingResponseDto
{
    public Guid Id { get; set; }
    public required string Reference { get; set; }
    public Guid UserId { get; set; }
    public required FlightResponseDto Flight { get; set; }
    public CabinClass Cabin { get; set; }
    public TripType TripType { get; set; }
    public BookingStatus Status { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public short PassengerCount { get; set; }
    public decimal BaseFare { get; set; }
    public decimal TaxesFees { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime BookedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<BookingPassengerResponseDto> Passengers { get; set; } = [];
}
