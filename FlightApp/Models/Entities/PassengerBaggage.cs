namespace FlightKS.Models.Entities;

public class PassengerBaggage
{
    public Guid Id { get; set; }

    public Guid PassengerSegmentId { get; set; }
    public PassengerSegment PassengerSegment { get; set; } = null!;

    public Guid BaggageOptionId { get; set; }
    public BaggageOption BaggageOption { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal PriceAtBooking { get; set; } // Snapshot — preserves the fee at the time of purchase
}
