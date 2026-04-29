namespace FlightKS.Models.Entities;

public class FlightFare
{
    public Guid Id { get; set; }

    public Guid FlightId { get; set; }
    public Flight Flight { get; set; } = null!;

    public CabinClass CabinClass { get; set; }
    public required string FareName { get; set; } // e.g. "Basic", "Standard", "Flex"
    public decimal BasePrice { get; set; }
    public decimal Taxes { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public bool IsRefundable { get; set; }
    public bool IsChangeable { get; set; }

    public ICollection<BaggageOption> IncludedBaggage { get; set; } = [];
    public ICollection<BookingSegment> BookingSegments { get; set; } = [];
}
