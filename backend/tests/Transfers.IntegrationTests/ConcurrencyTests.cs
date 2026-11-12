using System.Net;
using Transfers.Domain.Transfers;
using Transfers.IntegrationTests.Infrastructure;

namespace Transfers.IntegrationTests;

public sealed class ConcurrencyTests(PostgresFixture postgres) : IntegrationTest(postgres)
{
    private static readonly object GenerousLimit = new { maxAmount = 1_000_000m, maxAttempts = 1_000 };

    [Fact]
    public async Task Two_simultaneous_transfers_cannot_spend_the_same_balance()
    {
        var source = await Api.OpenAccountAsync(balance: 1_000m);
        var first = await Api.OpenAccountAsync();
        var second = await Api.OpenAccountAsync();

        var responses = await Task.WhenAll(
            Api.TransferAsync(source.Id, first.Id, 800m),
            Api.TransferAsync(source.Id, second.Id, 800m));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.UnprocessableEntity);
        Assert.Equal(200m, (await Api.GetAccountAsync(source.Id)).Account.Balance);
    }

    [Fact]
    public async Task Many_simultaneous_transfers_never_overdraw_the_account()
    {
        var source = await Api.OpenAccountAsync(balance: 1_000m, overdraftLimit: 500m, daytimeLimit: GenerousLimit, nighttimeLimit: GenerousLimit);
        var destination = await Api.OpenAccountAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Api.TransferAsync(source.Id, destination.Id, 100m)));

        Assert.Equal(15, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
        Assert.Equal(-500m, (await Api.GetAccountAsync(source.Id)).Account.Balance);
        Assert.Equal(1_500m, (await Api.GetAccountAsync(destination.Id)).Account.Balance);
    }

    [Fact]
    public async Task Opposite_transfers_between_the_same_accounts_do_not_deadlock()
    {
        var alice = await Api.OpenAccountAsync(balance: 1_000m, daytimeLimit: GenerousLimit, nighttimeLimit: GenerousLimit);
        var bob = await Api.OpenAccountAsync(balance: 1_000m, daytimeLimit: GenerousLimit, nighttimeLimit: GenerousLimit);

        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(index => index % 2 == 0
            ? Api.TransferAsync(alice.Id, bob.Id, 10m)
            : Api.TransferAsync(bob.Id, alice.Id, 10m)));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
        Assert.Equal(1_000m, (await Api.GetAccountAsync(alice.Id)).Account.Balance);
        Assert.Equal(1_000m, (await Api.GetAccountAsync(bob.Id)).Account.Balance);
    }

    [Fact]
    public async Task Concurrent_attempts_respect_the_hourly_attempt_limit()
    {
        var source = await Api.OpenAccountAsync(balance: 1_000m);
        var destination = await Api.OpenAccountAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Api.TransferAsync(source.Id, destination.Id, 10m)));

        Assert.Equal(5, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
        var rejections = await Task.WhenAll(responses.Where(response => response.StatusCode != HttpStatusCode.Created).Select(ApiClient.ReadRejectionAsync));
        Assert.All(rejections, rejection => Assert.Equal(TransferFailureReason.HourlyAttemptLimitExceeded, rejection.Transfer.FailureReason));
    }

    [Fact]
    public async Task Cancelling_while_the_worker_runs_resolves_to_a_single_outcome()
    {
        var source = await Api.OpenAccountAsync(balance: 1_000m);
        var destination = await Api.OpenAccountAsync();
        var scheduled = await ApiClient.ReadTransferAsync(
            await Api.ScheduleAsync(source.Id, destination.Id, 100m, Factory.Clock.GetUtcNow().AddMinutes(5)));
        Factory.Clock.Advance(TimeSpan.FromMinutes(5));

        var cancel = Api.Http.PostAsync($"/api/transfers/{scheduled.Id}/cancel", null);
        var process = Factory.ProcessDueTransfersAsync();
        await Task.WhenAll(cancel, process);

        var final = await Api.GetTransferAsync(scheduled.Id);
        var balance = (await Api.GetAccountAsync(source.Id)).Account.Balance;
        Assert.True(
            (final.Status == TransferStatus.Cancelled && balance == 1_000m && (await cancel).StatusCode == HttpStatusCode.OK)
            || (final.Status == TransferStatus.Completed && balance == 900m && (await cancel).StatusCode == HttpStatusCode.Conflict));
    }
}
