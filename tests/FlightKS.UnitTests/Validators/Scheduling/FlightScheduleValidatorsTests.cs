using FlightKS.Models.Dtos.FlightSchedules;
using FlightKS.Validation.Scheduling;

namespace FlightKS.UnitTests.Validators.Scheduling;

public class FlightScheduleCreateValidatorTests
{
    private readonly FlightScheduleCreateValidator _sut = new();

    private static readonly DateTime _dep = new(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _arr = new(2026, 8, 1, 14, 0, 0, DateTimeKind.Utc);

    private static FlightScheduleCreateDto Valid() =>
        new(Guid.NewGuid(), Guid.NewGuid(), _dep, _arr, null, null, null);

    [Fact]
    public void Valid_Passes() =>
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyFlightId_Fails() =>
        _sut.TestValidate(Valid() with { FlightId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.FlightId);

    [Fact]
    public void EmptyAircraftId_Fails() =>
        _sut.TestValidate(Valid() with { AircraftId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.AircraftId);

    [Fact]
    public void DefaultDepartureTime_Fails() =>
        _sut.TestValidate(Valid() with { DepartureTime = default })
            .ShouldHaveValidationErrorFor(x => x.DepartureTime);

    [Fact]
    public void DefaultArrivalTime_Fails() =>
        _sut.TestValidate(Valid() with { ArrivalTime = default })
            .ShouldHaveValidationErrorFor(x => x.ArrivalTime);

    [Fact]
    public void ArrivalBeforeDeparture_Fails()
    {
        var result = _sut.TestValidate(Valid() with { ArrivalTime = _dep.AddHours(-1) });
        result.ShouldHaveValidationErrorFor(x => x.ArrivalTime)
              .WithErrorMessage("Arrival time must be after departure time.");
    }

    [Fact]
    public void ArrivalEqualDeparture_Fails() =>
        _sut.TestValidate(Valid() with { ArrivalTime = _dep })
            .ShouldHaveValidationErrorFor(x => x.ArrivalTime);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ZeroOrNegativeCurrentPrice_Fails(decimal price) =>
        _sut.TestValidate(Valid() with { CurrentPrice = price })
            .ShouldHaveValidationErrorFor(x => x.CurrentPrice);

    [Fact]
    public void NullCurrentPrice_Passes() =>
        _sut.TestValidate(Valid() with { CurrentPrice = null })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void ClassPriceZero_Fails()
    {
        var dto = Valid() with
        {
            ClassPrices = [new FlightScheduleClassPriceDto(FlightKS.Enums.SeatClass.Business, 0m)]
        };
        var result = _sut.TestValidate(dto);
        result.ShouldHaveValidationErrorFor("ClassPrices[0].Price");
    }

    [Fact]
    public void NullClassPrices_Passes() =>
        _sut.TestValidate(Valid() with { ClassPrices = null })
            .ShouldNotHaveAnyValidationErrors();
}

public class FlightScheduleUpdateValidatorTests
{
    private readonly FlightScheduleUpdateValidator _sut = new();

    private static readonly DateTime _dep = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _arr = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AllNull_Passes() =>
        _sut.TestValidate(new FlightScheduleUpdateDto(null, null, null, null, null, null, null, null))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void ArrivalAfterDepartureBothSet_Passes() =>
        _sut.TestValidate(new FlightScheduleUpdateDto(null, null, null, _dep, _arr, null, null, null))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void ArrivalBeforeDepartureBothSet_Fails()
    {
        var dto = new FlightScheduleUpdateDto(null, null, null, _dep, _dep.AddHours(-1), null, null, null);
        _sut.TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.ArrivalTime)
            .WithErrorMessage("Arrival time must be after departure time.");
    }

    [Fact]
    public void OnlyArrivalSet_SkipsArrivalRule() =>
        _sut.TestValidate(new FlightScheduleUpdateDto(null, null, null, null, _arr, null, null, null))
            .ShouldNotHaveValidationErrorFor(x => x.ArrivalTime);

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void ZeroOrNegativeCurrentPriceWhenSet_Fails(decimal price) =>
        _sut.TestValidate(new FlightScheduleUpdateDto(null, null, null, null, null, price, null, null))
            .ShouldHaveValidationErrorFor(x => x.CurrentPrice);

    [Fact]
    public void ClassPriceZeroWhenSet_Fails()
    {
        var dto = new FlightScheduleUpdateDto(
            null, null, null, null, null, null, null,
            [new FlightScheduleClassPriceDto(FlightKS.Enums.SeatClass.Economy, 0m)]);
        _sut.TestValidate(dto).ShouldHaveValidationErrorFor("ClassPrices[0].Price");
    }
}

public class FlightScheduleStatusUpdateValidatorTests
{
    private readonly FlightScheduleStatusUpdateValidator _sut = new();

    [Fact]
    public void AllNull_Passes() =>
        _sut.TestValidate(new FlightScheduleStatusUpdateDto(null, null, null, null, null))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyGateWhenSet_Fails() =>
        _sut.TestValidate(new FlightScheduleStatusUpdateDto(null, "", null, null, null))
            .ShouldHaveValidationErrorFor(x => x.Gate);

    [Fact]
    public void EmptyDelayReasonWhenSet_Fails() =>
        _sut.TestValidate(new FlightScheduleStatusUpdateDto(null, null, "", null, null))
            .ShouldHaveValidationErrorFor(x => x.DelayReason);

    [Fact]
    public void ArrivalBeforeDepartureBothSet_Fails()
    {
        var dep = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
        var arr = dep.AddHours(-2);
        var dto = new FlightScheduleStatusUpdateDto(null, null, null, dep, arr);
        _sut.TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.ArrivalTime);
    }
}
