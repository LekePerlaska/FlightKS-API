using FlightKS.Enums;
using FlightKS.Mappers;
using FlightKS.Models.Entities;

namespace FlightKS.UnitTests.Mappers;

public class ItineraryMappingTests
{
    private static readonly Airport _lhr = E.Airport("LHR", "Heathrow", "London", "UK", "Europe/London");
    private static readonly Airport _jfk = E.Airport("JFK", "JFK Airport", "New York", "USA", "America/New_York");
    private static readonly Airline _airline = E.Airline("BA");
    private static readonly Aircraft _aircraft = E.Aircraft(_airline);

    private (Itinerary itinerary, ItinerarySegment segment) BuildSingleSegmentItinerary(
        ICollection<FlightSchedulePrice>? prices = null)
    {
        var flight = E.Flight(_airline, _lhr, _jfk);
        var schedule = E.Schedule(flight, _aircraft, prices: prices ?? []);
        var itin = E.Itinerary(_lhr, _jfk);
        var seg = E.Segment(itin.Id, schedule, order: 1);
        itin.Segments = [seg];
        return (itin, seg);
    }

    [Fact]
    public void ToSearchResult_NoSeatClass_MapsScalarFields()
    {
        var (itin, _) = BuildSingleSegmentItinerary();

        var dto = itin.ToSearchResult();

        dto.Id.Should().Be(itin.Id);
        dto.OriginAirportId.Should().Be(_lhr.Id);
        dto.DestinationAirportId.Should().Be(_jfk.Id);
        dto.OriginAirport.Code.Should().Be("LHR");
        dto.DestinationAirport.Code.Should().Be("JFK");
        dto.DepartureTime.Should().Be(itin.DepartureTime);
        dto.ArrivalTime.Should().Be(itin.ArrivalTime);
        dto.TotalDurationMinutes.Should().Be(itin.TotalDurationMinutes);
        dto.TotalPrice.Should().Be(itin.TotalPrice);
        dto.StopsCount.Should().Be(itin.StopsCount);
        dto.IsActive.Should().Be(itin.IsActive);
        dto.CreatedAt.Should().Be(itin.CreatedAt);
        dto.UpdatedAt.Should().Be(itin.UpdatedAt);
        dto.SelectedSeatClass.Should().BeNull();
        dto.SelectedClassTotalPrice.Should().BeNull();
    }

    [Fact]
    public void ToSearchResult_WithSeatClass_UsesMatchingPrice()
    {
        var (itin, seg) = BuildSingleSegmentItinerary();
        var bizPrice = E.SchedulePrice(seg.FlightSchedule.Id, SeatClass.Business, 750m);
        seg.FlightSchedule.Prices = [bizPrice];

        var dto = itin.ToSearchResult(SeatClass.Business);

        dto.SelectedSeatClass.Should().Be(SeatClass.Business);
        dto.SelectedClassTotalPrice.Should().Be(750m);
    }

    [Fact]
    public void ToSearchResult_WithSeatClassNotInPrices_FallsBackToCurrentPrice()
    {
        var (itin, seg) = BuildSingleSegmentItinerary();
        seg.FlightSchedule.Prices = [];
        seg.FlightSchedule.CurrentPrice = 200m;

        var dto = itin.ToSearchResult(SeatClass.First);

        dto.SelectedClassTotalPrice.Should().Be(200m);
    }

    [Fact]
    public void ToSearchResult_SegmentsOrderedBySegmentOrder()
    {
        var flight = E.Flight(_airline, _lhr, _jfk);
        var s1 = E.Schedule(flight, _aircraft);
        var s2 = E.Schedule(flight, _aircraft);
        var itin = E.Itinerary(_lhr, _jfk);
        var seg2 = E.Segment(itin.Id, s2, order: 2);
        var seg1 = E.Segment(itin.Id, s1, order: 1);
        itin.Segments = [seg2, seg1];

        var dto = itin.ToSearchResult();

        dto.Segments[0].SegmentOrder.Should().Be(1);
        dto.Segments[1].SegmentOrder.Should().Be(2);
    }

    [Fact]
    public void SegmentToDto_MapsAllFields()
    {
        var (itin, seg) = BuildSingleSegmentItinerary();
        seg.LayoverMinutesAfterSegment = 45;

        var dto = seg.ToDto();

        dto.Id.Should().Be(seg.Id);
        dto.SegmentOrder.Should().Be(1);
        dto.LayoverMinutesAfterSegment.Should().Be(45);
        dto.FlightScheduleId.Should().Be(seg.FlightScheduleId);
        dto.FlightId.Should().Be(seg.FlightSchedule.FlightId);
        dto.FlightNumber.Should().Be(seg.FlightSchedule.Flight.FlightNumber);
        dto.Airline.Id.Should().Be(_airline.Id);
        dto.OriginAirport.Code.Should().Be("LHR");
        dto.DestinationAirport.Code.Should().Be("JFK");
        dto.DepartureTime.Should().Be(seg.FlightSchedule.DepartureTime);
        dto.ArrivalTime.Should().Be(seg.FlightSchedule.ArrivalTime);
        dto.Status.Should().Be(seg.FlightSchedule.Status);
        dto.AvailableSeats.Should().Be(seg.FlightSchedule.AvailableSeats);
        dto.CurrentPrice.Should().Be(seg.FlightSchedule.CurrentPrice);
    }
}
