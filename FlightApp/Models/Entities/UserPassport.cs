namespace FlightKS.Models.Entities;

public class UserPassport
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public required string PassportNumber { get; set; }
    public required string Nationality { get; set; }
    public DateOnly ExpiryDate { get; set; }
}
