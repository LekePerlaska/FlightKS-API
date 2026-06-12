using FlightKS.Enums;
using FlightKS.Mappers;
using FlightKS.Models.Entities;

namespace FlightKS.UnitTests.Mappers;

public class AircraftMappingTests
{
    [Fact]
    public void ToDto_MapsAllFields()
    {
        var airline = E.Airline();
        var aircraft = E.Aircraft(airline);

        var dto = aircraft.ToDto();

        dto.Id.Should().Be(aircraft.Id);
        dto.AirlineId.Should().Be(aircraft.AirlineId);
        dto.AirlineName.Should().Be(airline.Name);
        dto.Model.Should().Be(aircraft.Model);
        dto.RegistrationNumber.Should().Be(aircraft.RegistrationNumber);
        dto.TotalSeats.Should().Be(aircraft.TotalSeats);
        dto.IsActive.Should().Be(aircraft.IsActive);
        dto.CreatedAt.Should().Be(aircraft.CreatedAt);
        dto.UpdatedAt.Should().Be(aircraft.UpdatedAt);
    }

    [Fact]
    public void ToDto_NullAirlineNav_UsesEmptyString()
    {
        var aircraft = E.Aircraft(airline: null);

        var dto = aircraft.ToDto();

        dto.AirlineName.Should().Be(string.Empty);
    }

    [Fact]
    public void SeatToAdminDto_MapsAllFields()
    {
        var seat = E.Seat("12A", SeatClass.Business, isWindow: true, isAisle: false, extraLegroom: true);

        var dto = seat.ToAdminDto();

        dto.Id.Should().Be(seat.Id);
        dto.SeatNumber.Should().Be("12A");
        dto.SeatClass.Should().Be(SeatClass.Business);
        dto.IsWindow.Should().BeTrue();
        dto.IsAisle.Should().BeFalse();
        dto.ExtraLegroom.Should().BeTrue();
    }
}
