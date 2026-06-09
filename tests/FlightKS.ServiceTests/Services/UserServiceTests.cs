using FlightKS.Exceptions;
using FlightKS.Services;
using FlightKS.ServiceTests.Fixtures;

namespace FlightKS.ServiceTests.Services;

public class UserServiceTests(PostgresFixture fixture) : ServiceTestBase(fixture)
{
    private static UserService MakeSut(FlightKS.Data.AppDbContext db) => new(db);

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ThrowsConflictException()
    {
        await using var setupDb = CreateContext();
        await new SeedData(setupDb).UserAsync("dup@example.com");

        await using var db = CreateContext();
        await MakeSut(db).Invoking(s =>
                s.CreateAsync(Guid.NewGuid().ToString(), "Other User", "dup@example.com"))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateAsync_Valid_PersistsUser()
    {
        await using var db = CreateContext();
        var keycloakId = Guid.NewGuid().ToString();
        var user = await MakeSut(db).CreateAsync(keycloakId, "Alice Smith", "alice@test.com");

        user.Id.Should().NotBeEmpty();
        user.Email.Should().Be("alice@test.com");
        user.FullName.Should().Be("Alice Smith");
        user.KeycloakUserId.Should().Be(keycloakId);
    }

    [Fact]
    public async Task GetOrCreateAsync_ExistingUser_ReturnsSame()
    {
        await using var setupDb = CreateContext();
        var existing = await new SeedData(setupDb).UserAsync("existing@test.com");

        await using var db = CreateContext();
        var found = await MakeSut(db).GetOrCreateAsync(existing.KeycloakUserId, "existing@test.com", "Existing User");

        found.Id.Should().Be(existing.Id);
    }
}
