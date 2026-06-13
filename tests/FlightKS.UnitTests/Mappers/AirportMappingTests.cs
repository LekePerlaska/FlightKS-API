using FlightKS.Mappers;

namespace FlightKS.UnitTests.Mappers;

public class AirportMappingTests
{
    [Fact]
    public void ToDto_MapsAllFields()
    {
        var airport = E.Airport("DXB", "Dubai International", "Dubai", "UAE", "Asia/Dubai");

        var dto = airport.ToDto();

        dto.Id.Should().Be(airport.Id);
        dto.Code.Should().Be("DXB");
        dto.Name.Should().Be("Dubai International");
        dto.City.Should().Be("Dubai");
        dto.Country.Should().Be("UAE");
        dto.TimeZone.Should().Be("Asia/Dubai");
    }

    [Fact]
    public void ToAdminListItem_MapsAllFields()
    {
        var airport = E.Airport("CDG", "Charles de Gaulle", "Paris", "France", "Europe/Paris");

        var dto = airport.ToAdminListItem();

        dto.Id.Should().Be(airport.Id);
        dto.Code.Should().Be("CDG");
        dto.Name.Should().Be("Charles de Gaulle");
        dto.City.Should().Be("Paris");
        dto.Country.Should().Be("France");
        dto.TimeZone.Should().Be("Europe/Paris");
        dto.IsActive.Should().Be(airport.IsActive);
        dto.CreatedAt.Should().Be(airport.CreatedAt);
        dto.UpdatedAt.Should().Be(airport.UpdatedAt);
    }
}
