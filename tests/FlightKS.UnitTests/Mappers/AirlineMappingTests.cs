using FlightKS.Mappers;

namespace FlightKS.UnitTests.Mappers;

public class AirlineMappingTests
{
    [Fact]
    public void ToDto_MapsAllFields()
    {
        var logo = E.LogoFile("/uploads/ba.png");
        var airline = E.Airline("BA", "British Airways", "UK", logo);

        var dto = airline.ToDto();

        dto.Id.Should().Be(airline.Id);
        dto.Code.Should().Be("BA");
        dto.Name.Should().Be("British Airways");
        dto.Country.Should().Be("UK");
        dto.LogoFileId.Should().Be(logo.Id);
        dto.LogoUrl.Should().Be("/uploads/ba.png");
    }

    [Fact]
    public void ToDto_NullLogo_NullLogoUrl()
    {
        var airline = E.Airline(logo: null);

        var dto = airline.ToDto();

        dto.LogoFileId.Should().BeNull();
        dto.LogoUrl.Should().BeNull();
    }

    [Fact]
    public void ToAdminListItem_MapsAllFields()
    {
        var airline = E.Airline("EK", "Emirates", "UAE");

        var dto = airline.ToAdminListItem();

        dto.Id.Should().Be(airline.Id);
        dto.Code.Should().Be("EK");
        dto.Name.Should().Be("Emirates");
        dto.Country.Should().Be("UAE");
        dto.IsActive.Should().Be(airline.IsActive);
        dto.CreatedAt.Should().Be(airline.CreatedAt);
        dto.UpdatedAt.Should().Be(airline.UpdatedAt);
    }
}
