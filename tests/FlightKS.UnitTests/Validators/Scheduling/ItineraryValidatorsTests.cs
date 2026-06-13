using FlightKS.Models.Dtos.Itineraries;
using FlightKS.Validation.Scheduling;

namespace FlightKS.UnitTests.Validators.Scheduling;

public class ItineraryCreateValidatorTests
{
    private readonly ItineraryCreateValidator _sut = new();

    private static Guid Id1 { get; } = Guid.NewGuid();
    private static Guid Id2 { get; } = Guid.NewGuid();

    [Fact]
    public void SingleScheduleId_Passes() =>
        _sut.TestValidate(new ItineraryCreateDto([Id1]))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void MultipleUniqueIds_Passes() =>
        _sut.TestValidate(new ItineraryCreateDto([Id1, Id2]))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void NullFlightScheduleIds_Fails() =>
        _sut.TestValidate(new ItineraryCreateDto(null!))
            .ShouldHaveValidationErrorFor(x => x.FlightScheduleIds);

    [Fact]
    public void EmptyFlightScheduleIds_Fails() =>
        _sut.TestValidate(new ItineraryCreateDto([]))
            .ShouldHaveValidationErrorFor(x => x.FlightScheduleIds);

    [Fact]
    public void DuplicateIds_Fails()
    {
        var result = _sut.TestValidate(new ItineraryCreateDto([Id1, Id1]));
        result.ShouldHaveValidationErrorFor(x => x.FlightScheduleIds)
              .WithErrorMessage("An itinerary cannot use the same flight schedule twice.");
    }

    [Fact]
    public void EmptyGuidInList_Fails() =>
        _sut.TestValidate(new ItineraryCreateDto([Id1, Guid.Empty]))
            .ShouldHaveValidationErrorFor("FlightScheduleIds[1]");
}

public class ItineraryUpdateValidatorTests
{
    private readonly ItineraryUpdateValidator _sut = new();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidIsActive_Passes(bool active) =>
        _sut.TestValidate(new ItineraryUpdateDto(active))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void NullIsActive_Fails() =>
        _sut.TestValidate(new ItineraryUpdateDto(null))
            .ShouldHaveValidationErrorFor(x => x.IsActive)
            .WithErrorMessage("isActive is required.");
}

public class ItinerarySegmentCreateValidatorTests
{
    private readonly ItinerarySegmentCreateValidator _sut = new();

    private static ItinerarySegmentCreateDto Valid() => new(Guid.NewGuid(), 1, null);

    [Fact]
    public void Valid_Passes() =>
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyFlightScheduleId_Fails() =>
        _sut.TestValidate(Valid() with { FlightScheduleId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.FlightScheduleId);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SegmentOrderBelowOne_Fails(int order) =>
        _sut.TestValidate(Valid() with { SegmentOrder = order })
            .ShouldHaveValidationErrorFor(x => x.SegmentOrder);

    [Fact]
    public void NullLayover_Passes() =>
        _sut.TestValidate(Valid() with { LayoverMinutesAfterSegment = null })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void NegativeLayover_Fails() =>
        _sut.TestValidate(Valid() with { LayoverMinutesAfterSegment = -1 })
            .ShouldHaveValidationErrorFor(x => x.LayoverMinutesAfterSegment);

    [Fact]
    public void ZeroLayover_Passes() =>
        _sut.TestValidate(Valid() with { LayoverMinutesAfterSegment = 0 })
            .ShouldNotHaveAnyValidationErrors();
}

public class ItinerarySegmentUpdateValidatorTests
{
    private readonly ItinerarySegmentUpdateValidator _sut = new();

    [Fact]
    public void AllNull_Passes() =>
        _sut.TestValidate(new ItinerarySegmentUpdateDto(null, null, null))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyFlightScheduleIdWhenSet_Fails() =>
        _sut.TestValidate(new ItinerarySegmentUpdateDto(Guid.Empty, null, null))
            .ShouldHaveValidationErrorFor(x => x.FlightScheduleId);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SegmentOrderBelowOneWhenSet_Fails(int order) =>
        _sut.TestValidate(new ItinerarySegmentUpdateDto(null, order, null))
            .ShouldHaveValidationErrorFor(x => x.SegmentOrder);

    [Fact]
    public void NegativeLayoverWhenSet_Fails() =>
        _sut.TestValidate(new ItinerarySegmentUpdateDto(null, null, -1))
            .ShouldHaveValidationErrorFor(x => x.LayoverMinutesAfterSegment);
}
