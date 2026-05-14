namespace FlightKS.Models.Dtos.Flights;

public class FlightResponseDto
{
    public required string Id { get; set; }
    public required AirportDto Origin { get; set; }
    public required AirportDto Destination { get; set; }
    public required string Airline { get; set; }
    public required string FlightNumber { get; set; }
    public required string Currency { get; set; }
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public int DurationMinutes { get; set; }
    public short Stops { get; set; }
    public bool BaggageIncluded { get; set; }
    public bool Refundable { get; set; }
    public List<FlightPriceDto> Prices { get; set; } = [];
}
