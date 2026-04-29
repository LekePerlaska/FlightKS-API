namespace FlightKS.Models.Entities;

public class Aircraft
{
    public Guid Id { get; set; }
    public required string Registration { get; set; } // Tail number, e.g. "YU-APJ"
    public required string Model { get; set; }         // e.g. "Airbus A319"
    public required string Manufacturer { get; set; }  // e.g. "Airbus", "Boeing"
    public int EconomySeats { get; set; }
    public int BusinessSeats { get; set; }

    public ICollection<Flight> Flights { get; set; } = [];
}
