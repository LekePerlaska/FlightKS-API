using FlightKS.Enums;

namespace FlightKS.Models.Pricing;

// Default fare multipliers applied to a schedule's base (Economy) fare when
// seeding per-class prices. Used only to produce sensible defaults — once a
// FlightSchedulePrice row exists it is the source of truth and can be edited.
public static class CabinFareMultipliers
{
    public static readonly IReadOnlyDictionary<SeatClass, decimal> Default =
        new Dictionary<SeatClass, decimal>
        {
            [SeatClass.Economy] = 1.0m,
            [SeatClass.PremiumEconomy] = 1.5m,
            [SeatClass.Business] = 2.5m,
            [SeatClass.First] = 4.0m,
        };

    public static decimal For(SeatClass seatClass) =>
        Default.TryGetValue(seatClass, out var multiplier) ? multiplier : 1.0m;
}
