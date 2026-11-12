using Transfers.Domain.Transfers;
using Transfers.IntegrationTests.Infrastructure;

namespace Transfers.IntegrationTests;

public sealed class DomainEventOutboxTests(PostgresFixture postgres) : IntegrationTest(postgres)
{
    [Fact]
    public async Task Events_are_stored_with_the_transfer_and_delivered_once()
    {
        var source = await Api.OpenAccountAsync(balance: 1_000m);
        var destination = await Api.OpenAccountAsync();

        var transfer = await ApiClient.ReadTransferAsync(await Api.TransferAsync(source.Id, destination.Id, 250m));

        Assert.Empty(Factory.Events.Delivered);
        Assert.Equal(1, await Factory.ProcessOutboxAsync());
        var completed = Assert.IsType<TransferCompleted>(Assert.Single(Factory.Events.Delivered));
        Assert.Equal(transfer.Id, completed.TransferId);
        Assert.Equal(250m, completed.Amount);

        Assert.Equal(0, await Factory.ProcessOutboxAsync());
        Assert.Single(Factory.Events.Delivered);
    }

    [Fact]
    public async Task Rejected_operations_do_not_write_events()
    {
        var account = await Api.OpenAccountAsync(balance: 100m);

        await Api.TransferAsync(account.Id, account.Id, 10m);

        Assert.Equal(0, await Factory.ProcessOutboxAsync());
    }

    [Fact]
    public async Task A_failed_delivery_is_retried_on_the_next_batch()
    {
        var source = await Api.OpenAccountAsync(balance: 1_000m);
        var destination = await Api.OpenAccountAsync();
        var scheduled = await ApiClient.ReadTransferAsync(
            await Api.ScheduleAsync(source.Id, destination.Id, 100m, Factory.Clock.GetUtcNow().AddHours(1)));
        await Api.Http.PostAsync($"/api/transfers/{scheduled.Id}/cancel", null);

        Factory.Events.FailNextDeliveries(1);
        await Factory.ProcessOutboxAsync();
        Assert.Empty(Factory.Events.Delivered);

        await Factory.ProcessOutboxAsync();
        var cancelled = Assert.IsType<TransferCancelled>(Assert.Single(Factory.Events.Delivered));
        Assert.Equal(scheduled.Id, cancelled.TransferId);
    }
}
