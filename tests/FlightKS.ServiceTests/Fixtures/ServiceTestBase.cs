using FlightKS.Data;

namespace FlightKS.ServiceTests.Fixtures;

[Collection("Service")]
public abstract class ServiceTestBase : IAsyncLifetime
{
    protected readonly PostgresFixture Fixture;

    protected ServiceTestBase(PostgresFixture fixture) => Fixture = fixture;

    // Respawn truncates all tables before each test
    public Task InitializeAsync() => Fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    protected AppDbContext CreateContext() => Fixture.CreateContext();
}
