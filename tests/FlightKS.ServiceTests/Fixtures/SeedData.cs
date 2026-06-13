using FlightKS.Data;
using FlightKS.Enums;
using FlightKS.Models.Entities;

namespace FlightKS.ServiceTests.Fixtures;

/// <summary>
/// Seeds minimal valid entity graphs into the database for service tests.
/// Each method saves immediately so callers get back persisted entities with DB-generated IDs.
/// </summary>
internal sealed class SeedData(AppDbContext db)
{
    public async Task<User> UserAsync(
        string email = "user@example.com",
        string fullName = "Test User")
    {
        var user = new User
        {
            KeycloakUserId = Guid.NewGuid().ToString(),
            Email = email,
            FullName = fullName
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<Airport> AirportAsync(
        string code = "LHR",
        string name = "Heathrow",
        string city = "London",
        string country = "UK",
        string tz = "Europe/London")
    {
        var airport = new Airport
        {
            Code = code,
            Name = name,
            City = city,
            Country = country,
            TimeZone = tz
        };
        db.Airports.Add(airport);
        await db.SaveChangesAsync();
        return airport;
    }

    public async Task<Airline> AirlineAsync(string code = "BA", string name = "British Airways")
    {
        var airline = new Airline { Code = code, Name = name, Country = "UK" };
        db.Airlines.Add(airline);
        await db.SaveChangesAsync();
        return airline;
    }

    public async Task<Aircraft> AircraftAsync(Guid airlineId, string model = "Boeing 737")
    {
        var aircraft = new Aircraft
        {
            AirlineId = airlineId,
            Model = model,
            RegistrationNumber = $"TC-{Guid.NewGuid().ToString()[..4].ToUpper()}",
            TotalSeats = 180
        };
        db.Aircrafts.Add(aircraft);
        await db.SaveChangesAsync();
        return aircraft;
    }

    public async Task<Seat> SeatAsync(
        Guid aircraftId,
        string number = "1A",
        SeatClass seatClass = SeatClass.Economy)
    {
        var seat = new Seat
        {
            AircraftId = aircraftId,
            SeatNumber = number,
            SeatClass = seatClass
        };
        db.Seats.Add(seat);
        await db.SaveChangesAsync();
        return seat;
    }

    public async Task<Flight> FlightAsync(
        Guid airlineId,
        Guid originId,
        Guid destinationId,
        string number = "BA001",
        decimal basePrice = 150m,
        int durationMinutes = 120)
    {
        var flight = new Flight
        {
            AirlineId = airlineId,
            FlightNumber = number,
            OriginAirportId = originId,
            DestinationAirportId = destinationId,
            BasePrice = basePrice,
            DurationMinutes = durationMinutes
        };
        db.Flights.Add(flight);
        await db.SaveChangesAsync();
        return flight;
    }

    public async Task<FlightSchedule> ScheduleAsync(
        Guid flightId,
        Guid aircraftId,
        DateTime? dep = null,
        DateTime? arr = null,
        decimal currentPrice = 200m,
        int availableSeats = 150,
        FlightScheduleStatus status = FlightScheduleStatus.Scheduled)
    {
        var d = dep ?? new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
        var a = arr ?? d.AddHours(2);
        var schedule = new FlightSchedule
        {
            FlightId = flightId,
            AircraftId = aircraftId,
            DepartureTime = d,
            ArrivalTime = a,
            CurrentPrice = currentPrice,
            AvailableSeats = availableSeats,
            Status = status
        };
        db.FlightSchedules.Add(schedule);
        await db.SaveChangesAsync();
        return schedule;
    }

    public async Task<FlightSchedulePrice> SchedulePriceAsync(
        Guid scheduleId, SeatClass seatClass, decimal price)
    {
        var sp = new FlightSchedulePrice
        {
            FlightScheduleId = scheduleId,
            SeatClass = seatClass,
            Price = price
        };
        db.FlightSchedulePrices.Add(sp);
        await db.SaveChangesAsync();
        return sp;
    }

    // Returned by ItineraryAsync to avoid named-tuple inference issues at call sites.
    internal record ItineraryResult(Itinerary Itinerary, ItinerarySegment Segment);

    /// <summary>Creates an Itinerary + one ItinerarySegment for the given schedule.</summary>
    public async Task<ItineraryResult> ItineraryAsync(
        Guid originId,
        Guid destinationId,
        FlightSchedule schedule,
        decimal totalPrice = 200m)
    {
        var itin = new Itinerary
        {
            OriginAirportId = originId,
            DestinationAirportId = destinationId,
            DepartureTime = schedule.DepartureTime,
            ArrivalTime = schedule.ArrivalTime,
            TotalDurationMinutes = (int)(schedule.ArrivalTime - schedule.DepartureTime).TotalMinutes,
            TotalPrice = totalPrice,
            StopsCount = 0,
            IsActive = true
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
        return new ItineraryResult(itin, seg);
    }

    public async Task<Booking> BookingAsync(
        Guid userId,
        Guid itineraryId,
        decimal totalAmount = 200m,
        BookingStatus status = BookingStatus.Pending)
    {
        var booking = new Booking
        {
            UserId = userId,
            ItineraryId = itineraryId,
            BookingReference = $"BKG-TEST{Guid.NewGuid().ToString()[..4].ToUpper()}",
            Status = status,
            TotalAmount = totalAmount
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking;
    }

    public async Task<Passenger> PassengerAsync(Guid bookingId, string first = "Jane", string last = "Doe")
    {
        var p = new Passenger
        {
            BookingId = bookingId,
            FirstName = first,
            LastName = last,
            DateOfBirth = new DateOnly(1990, 5, 15)
        };
        db.Passengers.Add(p);
        await db.SaveChangesAsync();
        return p;
    }

    public async Task<FlightSeat> FlightSeatAsync(
        Guid seatId,
        Guid scheduleId,
        FlightSeatStatus status = FlightSeatStatus.Available,
        decimal price = 200m,
        DateTime? reservedUntil = null)
    {
        var fs = new FlightSeat
        {
            SeatId = seatId,
            FlightScheduleId = scheduleId,
            Status = status,
            Price = price,
            ReservedUntil = reservedUntil
        };
        db.FlightSeats.Add(fs);
        await db.SaveChangesAsync();
        return fs;
    }

    public async Task<Ticket> TicketAsync(
        Guid bookingId,
        Guid passengerId,
        Guid scheduleId,
        Guid? flightSeatId = null,
        decimal price = 200m)
    {
        var ticket = new Ticket
        {
            BookingId = bookingId,
            PassengerId = passengerId,
            FlightScheduleId = scheduleId,
            FlightSeatId = flightSeatId,
            TicketNumber = $"TKT-TEST{Guid.NewGuid().ToString()[..4].ToUpper()}",
            TicketStatus = TicketStatus.Issued,
            Price = price,
            IssuedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        return ticket;
    }

    public async Task<BaggageOption> BaggageOptionAsync(
        string name = "Cabin Bag", decimal price = 25m, decimal weightKg = 7m)
    {
        var opt = new BaggageOption { Name = name, Price = price, WeightKg = weightKg };
        db.BaggageOptions.Add(opt);
        await db.SaveChangesAsync();
        return opt;
    }

    public async Task<BookingBaggage> BookingBaggageAsync(
        Guid bookingId, Guid passengerId, Guid baggageOptionId, int qty = 1)
    {
        var bb = new BookingBaggage
        {
            BookingId = bookingId,
            PassengerId = passengerId,
            BaggageOptionId = baggageOptionId,
            Quantity = qty
        };
        db.BookingBaggage.Add(bb);
        await db.SaveChangesAsync();
        return bb;
    }

    public async Task<FlightKS.Models.Entities.Notification> NotificationAsync(
        Guid userId, string title = "Test", string message = "Msg", bool isRead = false)
    {
        var n = new FlightKS.Models.Entities.Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = "Info",
            IsRead = isRead
        };
        db.Notifications.Add(n);
        await db.SaveChangesAsync();
        return n;
    }

    public async Task<Payment> PaymentAsync(
        Guid bookingId, decimal amount,
        PaymentStatus status = PaymentStatus.Completed)
    {
        var p = new Payment
        {
            BookingId = bookingId,
            Amount = amount,
            PaymentMethod = PaymentMethod.Card,
            PaymentStatus = status,
            PaidAt = DateTime.UtcNow
        };
        db.Payments.Add(p);
        await db.SaveChangesAsync();
        return p;
    }
}
