using FlightKS.Enums;

namespace FlightKS.Models.Entities;

public class UserPreferences
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public CabinClass? PreferredCabin { get; set; }
    public string? PreferredMeal { get; set; }
    public SeatSide? PreferredSeat { get; set; }
    public string? PreferredCurrency { get; set; }  // USD, EUR, GBP
    public bool EmailNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; }
}
