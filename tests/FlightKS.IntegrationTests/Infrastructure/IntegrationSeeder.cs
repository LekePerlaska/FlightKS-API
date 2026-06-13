using FlightKS.Data;
using FlightKS.Enums;
using FlightKS.Models.Entities;

namespace FlightKS.IntegrationTests.Infrastructure;

/// <summary>
/// Thin data-seeder for integration tests — mirrors SeedData from FlightKS.ServiceTests
/// but uses the factory's DbContext which talks to the test container.
/// </summary>
internal sealed class IntegrationSeeder(AppDbContext db)
{
    public async Task<Airport> AirportAsync(
        string code = "LHR", string name = "Heathrow",
        string city = "London", string country = "UK", string tz = "Europe/London")
    {
        var a = new Airport { Code = code, Name = name, City = city, Country = country, TimeZone = tz };
        db.Airports.Add(a);
        await db.SaveChangesAsync();
        return a;
    }

    public async Task<Airline> AirlineAsync(string code = "BA", string name = "British Airways")
    {
        var a = new Airline { Code = code, Name = name, Country = "UK" };
        db.Airlines.Add(a);
        await db.SaveChangesAsync();
        return a;
    }

    public async Task<Aircraft> AircraftAsync(Guid airlineId, string model = "Boeing 737")
    {
        var a = new Aircraft
        {
            AirlineId = airlineId, Model = model,
            RegistrationNumber = $"TC-{Guid.NewGuid().ToString()[..4].ToUpper()}",
            TotalSeats = 180
        };
        db.Aircrafts.Add(a);
        await db.SaveChangesAsync();
        return a;
    }

    public async Task<Flight> FlightAsync(
        Guid airlineId, Guid originId, Guid destId,
        string number = "BA001", decimal basePrice = 150m)
    {
        var f = new Flight
        {
            AirlineId = airlineId, FlightNumber = number,
            OriginAirportId = originId, DestinationAirportId = destId,
            BasePrice = basePrice, DurationMinutes = 120
        };
        db.Flights.Add(f);
        await db.SaveChangesAsync();
        return f;
    }

    public async Task<FlightSchedule> ScheduleAsync(
        Guid flightId, Guid aircraftId,
        DateTime? dep = null, DateTime? arr = null, decimal price = 200m)
    {
        var d = dep ?? new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
        var a = arr ?? d.AddHours(2);
        var s = new FlightSchedule
        {
            FlightId = flightId, AircraftId = aircraftId,
            DepartureTime = d, ArrivalTime = a,
            CurrentPrice = price, AvailableSeats = 150,
            Status = FlightScheduleStatus.Scheduled
        };
        db.FlightSchedules.Add(s);
        await db.SaveChangesAsync();
        return s;
    }

    public async Task<(Itinerary Itinerary, ItinerarySegment Segment)> ItineraryAsync(
        Guid originId, Guid destId, FlightSchedule schedule, decimal totalPrice = 200m)
    {
        var itin = new Itinerary
        {
            OriginAirportId = originId, DestinationAirportId = destId,
            DepartureTime = schedule.DepartureTime, ArrivalTime = schedule.ArrivalTime,
            TotalDurationMinutes = (int)(schedule.ArrivalTime - schedule.DepartureTime).TotalMinutes,
            TotalPrice = totalPrice, StopsCount = 0, IsActive = true
        };
        db.Itineraries.Add(itin);
        await db.SaveChangesAsync();

        var seg = new ItinerarySegment
        {
            ItineraryId = itin.Id,
            FlightScheduleId = schedule.Id,
            SegmentOrder = 1
        };
        db.ItinerarySegments.Add(seg);
        await db.SaveChangesAsync();
        return (itin, seg);
    }
}
