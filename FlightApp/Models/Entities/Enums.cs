namespace FlightKS.Models.Entities;

public enum FlightStatus  { Scheduled, Delayed, Cancelled, Boarding, InFlight, Landed }
public enum CabinClass    { Economy, PremiumEconomy, Business, First }
public enum BookingStatus { Pending, Confirmed, Cancelled, Completed, Refunded }
public enum TripType      { OneWay, RoundTrip, MultiStop }
public enum PaymentStatus { Pending, Completed, Failed, Refunded }
public enum PaymentMethod { CreditCard, DebitCard, BankTransfer, PayPal }
public enum PassengerType { Adult, Child, Infant }
public enum BaggageType   { PersonalItem, CabinBag, CheckedBag }
