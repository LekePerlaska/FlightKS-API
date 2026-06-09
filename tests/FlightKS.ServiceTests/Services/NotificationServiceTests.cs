using FlightKS.Services;
using FlightKS.ServiceTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.ServiceTests.Services;

public class NotificationServiceTests(PostgresFixture fixture) : ServiceTestBase(fixture)
{
    private static NotificationService MakeSut(FlightKS.Data.AppDbContext db) => new(db);

    [Fact]
    public async Task GetForUserAsync_ReturnsOnlyOwnedNotifications()
    {
        await using var setupDb = CreateContext();
        var seed = new SeedData(setupDb);
        var user1 = await seed.UserAsync("n1@test.com");
        var user2 = await seed.UserAsync("n2@test.com");
        await seed.NotificationAsync(user1.Id, "For user1");
        await seed.NotificationAsync(user2.Id, "For user2");

        await using var db = CreateContext();
        var (items, total) = await MakeSut(db).GetForUserAsync(user1.Id, unreadOnly: false, page: 1, pageSize: 10);

        total.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].Title.Should().Be("For user1");
    }

    [Fact]
    public async Task MarkAllReadAsync_MarksUnreadAndReturnsCount()
    {
        await using var setupDb = CreateContext();
        var seed = new SeedData(setupDb);
        var user = await seed.UserAsync("mra@test.com");
        await seed.NotificationAsync(user.Id, isRead: false);
        await seed.NotificationAsync(user.Id, isRead: false);
        await seed.NotificationAsync(user.Id, isRead: true); // already read — should not count

        await using var db = CreateContext();
        var count = await MakeSut(db).MarkAllReadAsync(user.Id);

        count.Should().Be(2);
        var allNotifs = await db.Notifications.Where(n => n.UserId == user.Id).ToListAsync();
        allNotifs.Should().AllSatisfy(n => n.IsRead.Should().BeTrue());
    }

    [Fact]
    public async Task DeleteAsync_OwnedNotification_DeletesIt()
    {
        await using var setupDb = CreateContext();
        var seed = new SeedData(setupDb);
        var user = await seed.UserAsync("del@test.com");
        var notif = await seed.NotificationAsync(user.Id);

        await using var db = CreateContext();
        await MakeSut(db).DeleteAsync(notif.Id, user.Id);

        var found = await db.Notifications.FindAsync(notif.Id);
        found.Should().BeNull();
    }

    [Fact]
    public async Task GetForUserAsync_UnreadOnlyFilter_ExcludesRead()
    {
        await using var setupDb = CreateContext();
        var seed = new SeedData(setupDb);
        var user = await seed.UserAsync("uo@test.com");
        await seed.NotificationAsync(user.Id, "Unread", isRead: false);
        await seed.NotificationAsync(user.Id, "Read", isRead: true);

        await using var db = CreateContext();
        var (items, total) = await MakeSut(db).GetForUserAsync(user.Id, unreadOnly: true, page: 1, pageSize: 10);

        total.Should().Be(1);
        items[0].Title.Should().Be("Unread");
    }
}
