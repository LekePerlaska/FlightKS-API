using FlightKS.Mappers;

namespace FlightKS.UnitTests.Mappers;

public class FlightMappingTests
{
    private static readonly FlightKS.Models.Entities.Airport _lhr = E.Airport("LHR", "Heathrow", "London", "UK", "Europe/London");
    private static readonly FlightKS.Models.Entities.Airport _dxb = E.Airport("DXB", "Dubai Int'l", "Dubai", "UAE", "Asia/Dubai");
    private static readonly FlightKS.Models.Entities.Airline _airline = E.Airline("BA", "British Airways", "UK");
    private static readonly FlightKS.Models.Entities.Aircraft _aircraft = E.Aircraft(_airline);

    [Fact]
    public void ToSearchResult_MapsAllFields()
    {
        var flight = E.Flight(_airline, _lhr, _dxb, "BA007", 250m, 420);
        var dep = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);
        var arr = dep.AddHours(7);
        var schedule = E.Schedule(flight, _aircraft, dep, arr, price: 350m, availableSeats: 90);

        var dto = schedule.ToSearchResult();

        dto.ScheduleId.Should().Be(schedule.Id);
        dto.FlightId.Should().Be(schedule.FlightId);
        dto.FlightNumber.Should().Be("BA007");
        dto.Airline.Id.Should().Be(_airline.Id);
        dto.Origin.Code.Should().Be("LHR");
        dto.Destination.Code.Should().Be("DXB");
        dto.DepartureTime.Should().Be(dep);
        dto.ArrivalTime.Should().Be(arr);
        dto.DurationMinutes.Should().Be(420);
        dto.CurrentPrice.Should().Be(350m);
        dto.AvailableSeats.Should().Be(90);
    }

    [Fact]
    public void ToSearchResult_NullAirline_Throws()
    {
        var flight = E.Flight(_airline, _lhr, _dxb);
        flight.Airline = null!;
        var schedule = E.Schedule(flight, _aircraft);

        var act = () => schedule.ToSearchResult();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ToAdminListItem_MapsAllFields()
    {
        var flight = E.Flight(_airline, _lhr, _dxb, "BA001", 199m, 360);

        var dto = flight.ToAdminListItem();

        dto.Id.Should().Be(flight.Id);
        dto.FlightNumber.Should().Be("BA001");
        dto.AirlineId.Should().Be(_airline.Id);
        dto.AirlineName.Should().Be("British Airways");
        dto.AirlineCode.Should().Be("BA");
        dto.Origin.Code.Should().Be("LHR");
        dto.Destination.Code.Should().Be("DXB");
        dto.BasePrice.Should().Be(199m);
        dto.DurationMinutes.Should().Be(360);
        dto.IsActive.Should().Be(flight.IsActive);
        dto.CreatedAt.Should().Be(flight.CreatedAt);
        dto.UpdatedAt.Should().Be(flight.UpdatedAt);
    }

    [Fact]
    public void ToAdminListItem_NullAirlineNav_UsesEmptyStrings()
    {
        var flight = E.Flight(_airline, _lhr, _dxb);
        flight.Airline = null!;

        var dto = flight.ToAdminListItem();

        dto.AirlineName.Should().Be(string.Empty);
        dto.AirlineCode.Should().Be(string.Empty);
    }

    [Fact]
    public void DurationMinutes_ArrivalBeforeDeparture_ReturnsZero()
    {
        var dep = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);
        var arr = dep.AddHours(-1);
        var flight = E.Flight(_airline, _lhr, _dxb);
        var schedule = E.Schedule(flight, _aircraft, dep, arr);

        var dto = schedule.ToSearchResult();

        dto.DurationMinutes.Should().Be(0);
    }
}
