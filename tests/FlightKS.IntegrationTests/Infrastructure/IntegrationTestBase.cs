using FlightKS.Data;

namespace FlightKS.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests.
/// Resets the DB before each test and seeds the canonical test user so
/// RequireCurrentUserFilter can resolve it from the JWT sub claim.
/// </summary>
[Collection("Integration")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly IntegrationWebAppFactory Factory;

    /// <summary>User-facing HTTP client — authenticates as the default test user with role "User".</summary>
    protected HttpClient Client { get; private set; } = null!;

    /// <summary>DB ID of the seeded test user (matches TestAuthHandler.TestKeycloakId).</summary>
    protected Guid TestUserId { get; private set; }

    protected IntegrationTestBase(IntegrationWebAppFactory factory) => Factory = factory;

    public virtual async Task InitializeAsync()
    {
        await Factory.ResetAsync();
        TestUserId = await Factory.SeedTestUserAsync();
        Client = Factory.CreateAuthenticatedClient();
    }

    public virtual Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>Opens a fresh DbContext pointing at the test container (for setup/verification).</summary>
    protected AppDbContext CreateDb() => Factory.CreateDbContext();

    /// <summary>Creates an anonymous (unauthenticated) client for testing 401 scenarios.</summary>
    protected HttpClient CreateAnonymousClient() => Factory.CreateAnonymousClient();

    /// <summary>Creates a client authenticated as a user with the given roles.</summary>
    protected HttpClient CreateClientWithRoles(params string[] roles) =>
        Factory.CreateAuthenticatedClient(roles);
}
