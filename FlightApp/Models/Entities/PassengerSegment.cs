namespace FlightKS.Models.Entities;

public class PassengerSegment
{
    public Guid Id { get; set; }

    public Guid PassengerId { get; set; }
    public Passenger Passenger { get; set; } = null!;

    public Guid BookingSegmentId { get; set; }
    public BookingSegment BookingSegment { get; set; } = null!;

    public string? SeatNumber { get; set; } // e.g. "14A"; null until seat is assigned

    public ICollection<PassengerBaggage> Baggage { get; set; } = [];
}
