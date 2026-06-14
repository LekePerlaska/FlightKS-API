using FlightKS.Enums;
using FlightKS.Mappers;
using FlightKS.Models.Entities;

namespace FlightKS.UnitTests.Mappers;

public class FlightScheduleMappingTests
{
    private static readonly Airport _lhr = E.Airport("LHR", "Heathrow", "London", "UK", "Europe/London");
    private static readonly Airport _jfk = E.Airport("JFK", "JFK Airport", "New York", "USA", "America/New_York");
    private static readonly Airline _airline = E.Airline("BA", "British Airways", "UK");
    private static readonly Aircraft _aircraft = E.Aircraft(_airline);

    private FlightSchedule MakeSchedule(
        DateTime? dep = null, DateTime? arr = null,
        string? gate = null, string? delayReason = null,
        FlightScheduleStatus status = FlightScheduleStatus.Scheduled,
        ICollection<FlightSchedulePrice>? prices = null)
    {
        var flight = E.Flight(_airline, _lhr, _jfk, "BA117");
        var d = dep ?? new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
        var a = arr ?? d.AddHours(8);
        return E.Schedule(flight, _aircraft, d, a,
            gate: gate, delayReason: delayReason,
            status: status, prices: prices ?? []);
    }

    [Fact]
    public void ToDetail_MapsAllFields()
    {
        var dep = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
        var arr = dep.AddHours(8);
        var s = MakeSchedule(dep, arr, gate: "B22", delayReason: null, status: FlightScheduleStatus.Scheduled);

        var dto = s.ToDetail();

        dto.Id.Should().Be(s.Id);
        dto.FlightId.Should().Be(s.FlightId);
        dto.FlightNumber.Should().Be("BA117");
        dto.Airline.Id.Should().Be(_airline.Id);
        dto.Origin.Code.Should().Be("LHR");
        dto.Destination.Code.Should().Be("JFK");
        dto.Aircraft.Id.Should().Be(_aircraft.Id);
        dto.DepartureTime.Should().Be(dep);
        dto.ArrivalTime.Should().Be(arr);
        dto.DurationMinutes.Should().Be(480);
        dto.Status.Should().Be(FlightScheduleStatus.Scheduled);
        dto.AvailableSeats.Should().Be(s.AvailableSeats);
        dto.CurrentPrice.Should().Be(s.CurrentPrice);
        dto.Gate.Should().Be("B22");
        dto.DelayReason.Should().BeNull();
    }

    [Fact]
    public void ToAdminListItem_MapsAllFields()
    {
        var prices = new List<FlightSchedulePrice>();
        var s = MakeSchedule(prices: prices);
        prices.Add(E.SchedulePrice(s.Id, SeatClass.Economy, 150m));
        prices.Add(E.SchedulePrice(s.Id, SeatClass.Business, 500m));
        s.Prices = prices;

        var dto = s.ToAdminListItem();

        dto.FlightNumber.Should().Be("BA117");
        dto.AirlineName.Should().Be("British Airways");
        dto.AirlineCode.Should().Be("BA");
        dto.OriginCode.Should().Be("LHR");
        dto.OriginTimeZone.Should().Be("Europe/London");
        dto.DestinationCode.Should().Be("JFK");
        dto.DestinationTimeZone.Should().Be("America/New_York");
        dto.AircraftId.Should().Be(_aircraft.Id);
        dto.AircraftModel.Should().Be(_aircraft.Model);
        dto.ClassPrices.Should().HaveCount(2);
        dto.ClassPrices[0].SeatClass.Should().Be(SeatClass.Economy);
        dto.ClassPrices[1].SeatClass.Should().Be(SeatClass.Business);
    }

    [Fact]
    public void ToManagerListItem_MapsAllFields()
    {
        var s = MakeSchedule(gate: "C3");

        var dto = s.ToManagerListItem();

        dto.Id.Should().Be(s.Id);
        dto.FlightId.Should().Be(s.FlightId);
        dto.FlightNumber.Should().Be("BA117");
        dto.OriginCode.Should().Be("LHR");
        dto.DestinationCode.Should().Be("JFK");
        dto.DepartureTime.Should().Be(s.DepartureTime);
        dto.ArrivalTime.Should().Be(s.ArrivalTime);
        dto.Status.Should().Be(s.Status);
        dto.AvailableSeats.Should().Be(s.AvailableSeats);
        dto.Gate.Should().Be("C3");
    }

    [Fact]
    public void FlightSeatToDto_MapsAllFields()
    {
        var seat = E.Seat("14C", SeatClass.Business, isWindow: true, isAisle: false, extraLegroom: false);
        var reserved = DateTime.UtcNow.AddHours(1);
        var fs = E.FlightSeat(seat, price: 450m, status: FlightSeatStatus.Reserved, reservedUntil: reserved);

        var dto = fs.ToDto();

        dto.Id.Should().Be(fs.Id);
        dto.SeatId.Should().Be(seat.Id);
        dto.SeatNumber.Should().Be("14C");
        dto.SeatClass.Should().Be(SeatClass.Business);
        dto.IsWindow.Should().BeTrue();
        dto.IsAisle.Should().BeFalse();
        dto.ExtraLegroom.Should().BeFalse();
        dto.Status.Should().Be(FlightSeatStatus.Reserved);
        dto.Price.Should().Be(450m);
        dto.ReservedUntil.Should().Be(reserved);
    }

    [Fact]
    public void SeatToScheduleSeatDto_MapsAllFields()
    {
        var seat = E.Seat("3A", SeatClass.First, isWindow: true, isAisle: false, extraLegroom: true);

        var dto = seat.ToScheduleSeatDto(1200m, FlightSeatStatus.Available);

        dto.Id.Should().Be(seat.Id);
        dto.SeatNumber.Should().Be("3A");
        dto.SeatClass.Should().Be(SeatClass.First);
        dto.IsWindow.Should().BeTrue();
        dto.ExtraLegroom.Should().BeTrue();
        dto.Status.Should().Be(FlightSeatStatus.Available);
        dto.Price.Should().Be(1200m);
    }

    [Fact]
    public void ClassPricesOrderedBySeatClass()
    {
        var prices = new List<FlightSchedulePrice>();
        var s = MakeSchedule(prices: prices);
        prices.Add(E.SchedulePrice(s.Id, SeatClass.First, 1000m));
        prices.Add(E.SchedulePrice(s.Id, SeatClass.Economy, 100m));
        prices.Add(E.SchedulePrice(s.Id, SeatClass.Business, 500m));
        s.Prices = prices;

        var dto = s.ToAdminListItem();

        dto.ClassPrices.Select(p => p.SeatClass).Should()
            .BeInAscendingOrder();
    }
}
