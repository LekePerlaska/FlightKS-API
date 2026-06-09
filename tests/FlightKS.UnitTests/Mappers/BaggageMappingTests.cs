using FlightKS.Mappers;

namespace FlightKS.UnitTests.Mappers;

public class BaggageMappingTests
{
    [Fact]
    public void ToDto_MapsAllFields()
    {
        var opt = E.BaggageOpt("Hold Bag", 23m, 45m);
        opt.Description = "Standard 23kg hold bag";

        var dto = opt.ToDto();

        dto.Id.Should().Be(opt.Id);
        dto.Name.Should().Be("Hold Bag");
        dto.WeightKg.Should().Be(23m);
        dto.Price.Should().Be(45m);
        dto.Description.Should().Be("Standard 23kg hold bag");
    }

    [Fact]
    public void ToDto_NullDescription_MapsNull()
    {
        var opt = E.BaggageOpt();
        opt.Description = null;

        opt.ToDto().Description.Should().BeNull();
    }

    [Fact]
    public void ToAdminListItem_MapsAllFields()
    {
        var opt = E.BaggageOpt("Cabin Bag", 7m, 15m);

        var dto = opt.ToAdminListItem();

        dto.Id.Should().Be(opt.Id);
        dto.Name.Should().Be("Cabin Bag");
        dto.WeightKg.Should().Be(7m);
        dto.Price.Should().Be(15m);
        dto.IsActive.Should().Be(opt.IsActive);
        dto.CreatedAt.Should().Be(opt.CreatedAt);
        dto.UpdatedAt.Should().Be(opt.UpdatedAt);
    }
}
