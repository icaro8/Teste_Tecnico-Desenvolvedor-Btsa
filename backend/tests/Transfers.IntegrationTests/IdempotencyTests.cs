using System.Net;
using Transfers.IntegrationTests.Infrastructure;

namespace Transfers.IntegrationTests;

public sealed class IdempotencyTests(PostgresFixture postgres) : IntegrationTest(postgres)
{
    [Fact]
    public async Task Repeating_a_request_with_the_same_key_does_not_transfer_twice()
    {
        var source = await Api.OpenAccountAsync(balance: 1_000m);
        var destination = await Api.OpenAccountAsync();
        var key = Guid.NewGuid().ToString();

        var first = await Api.TransferAsync(source.Id, destination.Id, 100m, key);
        var retry = await Api.TransferAsync(source.Id, destination.Id, 100m, key);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal("true", retry.Headers.GetValues("Idempotent-Replayed").Single());
        Assert.Equal((await ApiClient.ReadTransferAsync(first)).Id, (await ApiClient.ReadTransferAsync(retry)).Id);
        Assert.Equal(900m, (await Api.GetAccountAsync(source.Id)).Account.Balance);
    }

    [Fact]
    public async Task Simultaneous_retries_with_the_same_key_create_one_transfer()
    {
        var source = await Api.OpenAccountAsync(balance: 1_000m);
        var destination = await Api.OpenAccountAsync();
        var key = Guid.NewGuid().ToString();

        var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Api.TransferAsync(source.Id, destination.Id, 100m, key)));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Equal(900m, (await Api.GetAccountAsync(source.Id)).Account.Balance);
    }

    [Fact]
    public async Task Reusing_a_key_with_different_terms_is_a_conflict()
    {
        var source = await Api.OpenAccountAsync(balance: 1_000m);
        var destination = await Api.OpenAccountAsync();
        var key = Guid.NewGuid().ToString();

        await Api.TransferAsync(source.Id, destination.Id, 100m, key);
        var response = await Api.TransferAsync(source.Id, destination.Id, 200m, key);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
