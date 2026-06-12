using FlightKS.Models.Dtos.Flights;
using FlightKS.Validation.Scheduling;

namespace FlightKS.UnitTests.Validators.Scheduling;

public class FlightCreateValidatorTests
{
    private readonly FlightCreateValidator _sut = new();

    private static readonly Guid _airlineId = Guid.NewGuid();
    private static readonly Guid _originId = Guid.NewGuid();
    private static readonly Guid _destId = Guid.NewGuid();

    private static FlightCreateDto Valid() => new(_airlineId, "BA001", _originId, _destId, 150m);

    [Fact]
    public void Valid_Passes() =>
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyAirlineId_Fails() =>
        _sut.TestValidate(Valid() with { AirlineId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.AirlineId);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyFlightNumber_Fails(string fn) =>
        _sut.TestValidate(Valid() with { FlightNumber = fn })
            .ShouldHaveValidationErrorFor(x => x.FlightNumber);

    [Fact]
    public void FlightNumberTooLong_Fails() =>
        _sut.TestValidate(Valid() with { FlightNumber = new string('X', 11) })
            .ShouldHaveValidationErrorFor(x => x.FlightNumber);

    [Fact]
    public void EmptyOriginAirportId_Fails() =>
        _sut.TestValidate(Valid() with { OriginAirportId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.OriginAirportId);

    [Fact]
    public void EmptyDestinationAirportId_Fails() =>
        _sut.TestValidate(Valid() with { DestinationAirportId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.DestinationAirportId);

    [Fact]
    public void SameOriginAndDestination_Fails()
    {
        var result = _sut.TestValidate(Valid() with { OriginAirportId = _destId });
        result.ShouldHaveValidationErrorFor(x => x.DestinationAirportId)
              .WithErrorMessage("Origin and destination airports must differ.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BasePriceNotPositive_Fails(decimal price) =>
        _sut.TestValidate(Valid() with { BasePrice = price })
            .ShouldHaveValidationErrorFor(x => x.BasePrice);
}

public class FlightUpdateValidatorTests
{
    private readonly FlightUpdateValidator _sut = new();

    [Fact]
    public void AllNull_Passes() =>
        _sut.TestValidate(new FlightUpdateDto(null, null, null, null, null, null))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyAirlineIdWhenSet_Fails() =>
        _sut.TestValidate(new FlightUpdateDto(Guid.Empty, null, null, null, null, null))
            .ShouldHaveValidationErrorFor(x => x.AirlineId);

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void BasePriceNotPositiveWhenSet_Fails(decimal price) =>
        _sut.TestValidate(new FlightUpdateDto(null, null, null, null, price, null))
            .ShouldHaveValidationErrorFor(x => x.BasePrice);

    [Fact]
    public void EmptyFlightNumberWhenSet_Fails() =>
        _sut.TestValidate(new FlightUpdateDto(null, "", null, null, null, null))
            .ShouldHaveValidationErrorFor(x => x.FlightNumber);
}
