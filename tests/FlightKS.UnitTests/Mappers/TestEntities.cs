using FlightKS.Enums;
using FlightKS.Models.Entities;

namespace FlightKS.UnitTests.Mappers;

internal static class E
{
    internal static Airport Airport(
        string code = "LHR", string name = "Heathrow",
        string city = "London", string country = "UK",
        string tz = "Europe/London") => new()
    {
        Id = Guid.NewGuid(), Code = code, Name = name,
        City = city, Country = country, TimeZone = tz
    };

    internal static UploadedFile LogoFile(string path = "/uploads/logo.png") => new()
    {
        Id = Guid.NewGuid(), UploadedByUserId = Guid.NewGuid(),
        FileName = "logo.png", OriginalFileName = "logo.png",
        ContentType = "image/png", SizeBytes = 1024,
        StoragePath = path
    };

    internal static Airline Airline(
        string code = "BA", string name = "British Airways",
        string country = "UK", UploadedFile? logo = null) => new()
    {
        Id = Guid.NewGuid(), Code = code, Name = name, Country = country,
        LogoFileId = logo?.Id, LogoFile = logo
    };

    internal static Aircraft Aircraft(Airline? airline = null) => new()
    {
        Id = Guid.NewGuid(),
        AirlineId = airline?.Id ?? Guid.NewGuid(),
        Airline = airline!,
        Model = "Boeing 737", RegistrationNumber = "TC-JFA", TotalSeats = 180
    };

    internal static Seat Seat(
        string number = "1A", SeatClass seatClass = SeatClass.Economy,
        bool isWindow = false, bool isAisle = false, bool extraLegroom = false) => new()
    {
        Id = Guid.NewGuid(), SeatNumber = number, SeatClass = seatClass,
        IsWindow = isWindow, IsAisle = isAisle, ExtraLegroom = extraLegroom
    };

    internal static FlightSeat FlightSeat(
        Seat seat, decimal price = 100m,
        FlightSeatStatus status = FlightSeatStatus.Available,
        DateTime? reservedUntil = null) => new()
    {
        Id = Guid.NewGuid(), SeatId = seat.Id, Seat = seat,
        Price = price, Status = status, ReservedUntil = reservedUntil
    };

    internal static Flight Flight(
        Airline airline, Airport origin, Airport dest,
        string number = "BA001", decimal basePrice = 150m, int durationMinutes = 120) => new()
    {
        Id = Guid.NewGuid(),
        AirlineId = airline.Id, Airline = airline,
        FlightNumber = number,
        OriginAirportId = origin.Id, OriginAirport = origin,
        DestinationAirportId = dest.Id, DestinationAirport = dest,
        BasePrice = basePrice, DurationMinutes = durationMinutes
    };

    internal static FlightSchedule Schedule(
        Flight flight, Aircraft aircraft,
        DateTime? dep = null, DateTime? arr = null,
        decimal price = 200m, int availableSeats = 150,
        FlightScheduleStatus status = FlightScheduleStatus.Scheduled,
        string? gate = null, string? delayReason = null,
        ICollection<FlightSchedulePrice>? prices = null)
    {
        var d = dep ?? new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);
        var a = arr ?? d.AddHours(2);
        return new FlightSchedule
        {
            Id = Guid.NewGuid(),
            FlightId = flight.Id, Flight = flight,
            AircraftId = aircraft.Id, Aircraft = aircraft,
            DepartureTime = d, ArrivalTime = a,
            Status = status, AvailableSeats = availableSeats,
            CurrentPrice = price, Gate = gate, DelayReason = delayReason,
            Prices = prices ?? []
        };
    }

    internal static FlightSchedulePrice SchedulePrice(
        Guid scheduleId, SeatClass cls, decimal price) => new()
    {
        Id = Guid.NewGuid(), FlightScheduleId = scheduleId,
        SeatClass = cls, Price = price
    };

    internal static User User(
        string email = "test@example.com",
        string fullName = "Test User") => new()
    {
        Id = Guid.NewGuid(),
        KeycloakUserId = Guid.NewGuid().ToString(),
        Email = email, FullName = fullName
    };

    internal static Booking Booking(
        Guid userId, string reference = "REF001",
        BookingStatus status = BookingStatus.Pending, decimal total = 500m) => new()
    {
        Id = Guid.NewGuid(), UserId = userId,
        BookingReference = reference, Status = status, TotalAmount = total
    };

    internal static Passenger Passenger(
        Guid bookingId, string first = "Jane", string last = "Doe") => new()
    {
        Id = Guid.NewGuid(), BookingId = bookingId,
        FirstName = first, LastName = last,
        DateOfBirth = new DateOnly(1990, 5, 15)
    };

    internal static Payment Payment(
        Guid bookingId, decimal amount = 500m,
        PaymentStatus status = PaymentStatus.Completed,
        DateTime? createdAt = null) => new()
    {
        Id = Guid.NewGuid(), BookingId = bookingId, Amount = amount,
        PaymentMethod = PaymentMethod.Card, PaymentStatus = status,
        PaidAt = DateTime.UtcNow,
        CreatedAt = createdAt ?? DateTime.UtcNow
    };

    internal static BaggageOption BaggageOpt(
        string name = "Cabin Bag", decimal weight = 7m, decimal price = 20m) => new()
    {
        Id = Guid.NewGuid(), Name = name, WeightKg = weight, Price = price
    };

    internal static BookingBaggage BookingBaggage(
        Guid bookingId, Guid passengerId, BaggageOption option, int qty = 1) => new()
    {
        Id = Guid.NewGuid(), BookingId = bookingId, PassengerId = passengerId,
        BaggageOptionId = option.Id, BaggageOption = option, Quantity = qty
    };

    internal static Notification Notification(
        Guid userId, string title = "Test", string message = "Msg",
        string type = "Info", bool isRead = false,
        string? relatedEntityName = null, Guid? relatedEntityId = null) => new()
    {
        Id = Guid.NewGuid(), UserId = userId,
        Title = title, Message = message, Type = type, IsRead = isRead,
        RelatedEntityName = relatedEntityName, RelatedEntityId = relatedEntityId
    };

    internal static Ticket Ticket(
        Guid bookingId, Guid passengerId,
        FlightSchedule schedule, Passenger passenger,
        string ticketNumber = "TK-001",
        decimal price = 200m,
        FlightSeat? flightSeat = null) => new()
    {
        Id = Guid.NewGuid(), BookingId = bookingId, PassengerId = passengerId,
        FlightScheduleId = schedule.Id, FlightSchedule = schedule,
        Passenger = passenger,
        FlightSeatId = flightSeat?.Id, FlightSeat = flightSeat,
        TicketNumber = ticketNumber, TicketStatus = TicketStatus.Issued,
        Price = price, IssuedAt = DateTime.UtcNow
    };

    internal static ItinerarySegment Segment(
        Guid itineraryId, FlightSchedule schedule,
        int order = 1, int? layover = null) => new()
    {
        Id = Guid.NewGuid(), ItineraryId = itineraryId,
        FlightScheduleId = schedule.Id, FlightSchedule = schedule,
        SegmentOrder = order, LayoverMinutesAfterSegment = layover
    };

    internal static Itinerary Itinerary(
        Airport origin, Airport dest,
        DateTime? dep = null, DateTime? arr = null,
        decimal totalPrice = 300m)
    {
        var d = dep ?? new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);
        var a = arr ?? d.AddHours(4);
        return new Itinerary
        {
            Id = Guid.NewGuid(),
            OriginAirportId = origin.Id, OriginAirport = origin,
            DestinationAirportId = dest.Id, DestinationAirport = dest,
            DepartureTime = d, ArrivalTime = a,
            TotalDurationMinutes = (int)(a - d).TotalMinutes,
            TotalPrice = totalPrice, StopsCount = 0
        };
    }

    internal static UploadedFile PassportDoc(Guid userId, DateTime? createdAt = null) => new()
    {
        Id = Guid.NewGuid(), UploadedByUserId = userId,
        FileName = "passport.pdf", OriginalFileName = "my_passport.pdf",
        ContentType = "application/pdf", SizeBytes = 12345,
        StoragePath = "/uploads/passport.pdf",
        RelatedEntityName = "UserPassportDocument",
        CreatedAt = createdAt ?? DateTime.UtcNow
    };
}
