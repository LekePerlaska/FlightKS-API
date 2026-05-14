namespace FlightKS.Models.Entities;

public class UserLoyalty
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public required string FrequentFlyerProgram { get; set; }
    public required string FrequentFlyerNumber { get; set; }
}
