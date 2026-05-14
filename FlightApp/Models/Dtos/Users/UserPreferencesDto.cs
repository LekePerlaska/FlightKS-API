using FlightKS.Enums;

namespace FlightKS.Models.Dtos.Users;

public class UserPreferencesDto
{
    public CabinClass? PreferredCabin { get; set; }
    public string? PreferredMeal { get; set; }
    public SeatSide? PreferredSeat { get; set; }
    public string? PreferredCurrency { get; set; }
    public bool EmailNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; }
}
