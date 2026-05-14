namespace FlightKS.Models.Entities;

public class Airport
{
    public required string Code { get; set; }        // CHAR(3) IATA, PK
    public required string City { get; set; }
    public required string Country { get; set; }
    public string? Name { get; set; }
    public string? Timezone { get; set; }
}
