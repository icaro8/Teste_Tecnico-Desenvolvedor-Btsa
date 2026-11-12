namespace Transfers.IntegrationTests.Infrastructure;

[Collection(IntegrationCollection.Name)]
public abstract class IntegrationTest : IAsyncLifetime
{
    protected IntegrationTest(PostgresFixture postgres)
    {
        Factory = new TransfersApiFactory(postgres);
        Api = new ApiClient(Factory.CreateClient());
    }

    protected TransfersApiFactory Factory { get; }

    protected ApiClient Api { get; }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        Npgsql.NpgsqlConnection.ClearAllPools();
    }
}
