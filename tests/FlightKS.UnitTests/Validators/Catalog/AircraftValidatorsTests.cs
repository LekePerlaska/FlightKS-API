using FlightKS.Enums;
using FlightKS.Models.Dtos.Aircrafts;
using FlightKS.Validation.Catalog;

namespace FlightKS.UnitTests.Validators.Catalog;

public class AircraftCreateValidatorTests
{
    private readonly AircraftCreateValidator _sut = new();

    private static AircraftCreateDto Valid() => new(Guid.NewGuid(), "Boeing 737", "TC-JFA", 180);

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
    public void EmptyModel_Fails(string model) =>
        _sut.TestValidate(Valid() with { Model = model })
            .ShouldHaveValidationErrorFor(x => x.Model);

    [Fact]
    public void ModelTooLong_Fails() =>
        _sut.TestValidate(Valid() with { Model = new string('a', 101) })
            .ShouldHaveValidationErrorFor(x => x.Model);

    [Fact]
    public void EmptyRegistrationNumber_Fails() =>
        _sut.TestValidate(Valid() with { RegistrationNumber = "" })
            .ShouldHaveValidationErrorFor(x => x.RegistrationNumber);

    [Fact]
    public void RegistrationNumberTooLong_Fails() =>
        _sut.TestValidate(Valid() with { RegistrationNumber = new string('X', 21) })
            .ShouldHaveValidationErrorFor(x => x.RegistrationNumber);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TotalSeatsNotPositive_Fails(int seats) =>
        _sut.TestValidate(Valid() with { TotalSeats = seats })
            .ShouldHaveValidationErrorFor(x => x.TotalSeats);
}

public class AircraftUpdateValidatorTests
{
    private readonly AircraftUpdateValidator _sut = new();

    [Fact]
    public void AllNull_Passes() =>
        _sut.TestValidate(new AircraftUpdateDto(null, null, null, null, null))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyAirlineIdWhenSet_Fails() =>
        _sut.TestValidate(new AircraftUpdateDto(Guid.Empty, null, null, null, null))
            .ShouldHaveValidationErrorFor(x => x.AirlineId);

    [Fact]
    public void EmptyModelWhenSet_Fails() =>
        _sut.TestValidate(new AircraftUpdateDto(null, "", null, null, null))
            .ShouldHaveValidationErrorFor(x => x.Model);

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void TotalSeatsNotPositiveWhenSet_Fails(int seats) =>
        _sut.TestValidate(new AircraftUpdateDto(null, null, null, seats, null))
            .ShouldHaveValidationErrorFor(x => x.TotalSeats);
}

public class SeatBatchCreateValidatorTests
{
    private readonly SeatBatchCreateValidator _sut = new();

    private static SeatCreateItemDto ValidSeat(string num = "1A") =>
        new(num, SeatClass.Economy, false, false, false);

    [Fact]
    public void ValidSeats_Passes() =>
        _sut.TestValidate(new SeatBatchCreateDto([ValidSeat("1A"), ValidSeat("1B")]))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void NullSeats_Fails() =>
        _sut.TestValidate(new SeatBatchCreateDto(null!))
            .ShouldHaveValidationErrorFor(x => x.Seats);

    [Fact]
    public void EmptySeats_Fails() =>
        _sut.TestValidate(new SeatBatchCreateDto([]))
            .ShouldHaveValidationErrorFor(x => x.Seats);

    [Fact]
    public void SeatWithEmptySeatNumber_Fails() =>
        _sut.TestValidate(new SeatBatchCreateDto([new("", SeatClass.Economy, false, false, false)]))
            .ShouldHaveValidationErrorFor("Seats[0].SeatNumber");

    [Fact]
    public void SeatNumberTooLong_Fails() =>
        _sut.TestValidate(new SeatBatchCreateDto([new(new string('A', 11), SeatClass.Economy, false, false, false)]))
            .ShouldHaveValidationErrorFor("Seats[0].SeatNumber");
}
