namespace FlightKS.Models.Dtos.Flights;

public class AirportDto
{
    public required string Code { get; set; }
    public required string City { get; set; }
    public required string Country { get; set; }
    public string? Name { get; set; }
    public string? Timezone { get; set; }
}
