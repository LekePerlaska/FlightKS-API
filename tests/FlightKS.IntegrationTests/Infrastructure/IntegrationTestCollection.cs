namespace FlightKS.IntegrationTests.Infrastructure;

[CollectionDefinition("Integration")]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationWebAppFactory>;
