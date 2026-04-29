namespace FlightKS.Models.Entities;

public class BaggageOption
{
    public Guid Id { get; set; }
    public required string Name { get; set; }     // e.g. "23kg Checked Bag", "Cabin Bag"
    public BaggageType Type { get; set; }
    public decimal? MaxWeightKg { get; set; }
    public string? MaxDimensions { get; set; }    // e.g. "55×40×20 cm"

    // Fee = 0 when this option is included in the linked FlightFare;
    // Fee > 0 when it is a purchasable add-on.
    public decimal Fee { get; set; }

    // Airline-wide option (null when fare-specific)
    public Guid? AirlineId { get; set; }
    public Airline? Airline { get; set; }

    // Fare-specific option (null when airline-wide)
    public Guid? FlightFareId { get; set; }
    public FlightFare? FlightFare { get; set; }

    public ICollection<PassengerBaggage> PassengerBaggages { get; set; } = [];
}
