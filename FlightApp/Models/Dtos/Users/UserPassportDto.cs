namespace FlightKS.Models.Dtos.Users;

public class UserPassportDto
{
    public required string PassportNumber { get; set; }
    public required string Nationality { get; set; }
    public DateOnly ExpiryDate { get; set; }
}
