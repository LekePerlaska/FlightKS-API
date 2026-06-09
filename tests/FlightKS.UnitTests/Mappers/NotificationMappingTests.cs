using FlightKS.Mappers;

namespace FlightKS.UnitTests.Mappers;

public class NotificationMappingTests
{
    [Fact]
    public void ToDto_MapsAllFields()
    {
        var relatedId = Guid.NewGuid();
        var n = E.Notification(
            Guid.NewGuid(), "Flight Delayed", "Your flight is delayed 30 min.",
            "FlightAlert", isRead: true,
            relatedEntityName: "FlightSchedule", relatedEntityId: relatedId);

        var dto = n.ToDto();

        dto.Id.Should().Be(n.Id);
        dto.Title.Should().Be("Flight Delayed");
        dto.Message.Should().Be("Your flight is delayed 30 min.");
        dto.Type.Should().Be("FlightAlert");
        dto.IsRead.Should().BeTrue();
        dto.RelatedEntityName.Should().Be("FlightSchedule");
        dto.RelatedEntityId.Should().Be(relatedId);
        dto.CreatedAt.Should().Be(n.CreatedAt);
    }

    [Fact]
    public void ToDto_NullRelatedFields_MapNull()
    {
        var n = E.Notification(Guid.NewGuid());

        var dto = n.ToDto();

        dto.RelatedEntityName.Should().BeNull();
        dto.RelatedEntityId.Should().BeNull();
    }
}
