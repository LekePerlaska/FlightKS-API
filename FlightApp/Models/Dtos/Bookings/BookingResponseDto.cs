using FlightKS.Enums;
using FlightKS.Models.Dtos.Passengers;
using FlightKS.Models.Dtos.Payments;

namespace FlightKS.Models.Dtos.Bookings;

public class BookingResponseDto
{
    public Guid Id { get; set; }
    public required string BookingReference { get; set; }
    public Guid UserId { get; set; }
    public BookingStatus Status { get; set; }
    public decimal TotalPrice { get; set; }
    public required string Currency { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<BookingFlightResponseDto> Flights { get; set; } = [];
    public List<PassengerResponseDto> Passengers { get; set; } = [];
    public PaymentResponseDto? Payment { get; set; }
}
