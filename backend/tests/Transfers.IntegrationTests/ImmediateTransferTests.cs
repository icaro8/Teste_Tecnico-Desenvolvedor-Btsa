using System.Net;
using Transfers.Domain.Transfers;
using Transfers.IntegrationTests.Infrastructure;

namespace Transfers.IntegrationTests;

public sealed class ImmediateTransferTests(PostgresFixture postgres) : IntegrationTest(postgres)
{
    [Fact]
    public async Task Transfer_with_sufficient_balance_completes()
    {
        var source = await Api.OpenAccountAsync(balance: 1_000m);
        var destination = await Api.OpenAccountAsync();

        var response = await Api.TransferAsync(source.Id, destination.Id, 300m);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var transfer = await ApiClient.ReadTransferAsync(response);
        Assert.Equal(TransferStatus.Completed, transfer.Status);
        Assert.Equal($"/api/transfers/{transfer.Id}", response.Headers.Location!.AbsolutePath);
        Assert.Equal(700m, (await Api.GetAccountAsync(source.Id)).Account.Balance);
        Assert.Equal(300m, (await Api.GetAccountAsync(destination.Id)).Account.Balance);
    }

    [Fact]
    public async Task Transfer_using_overdraft_leaves_negative_balance()
    {
        var source = await Api.OpenAccountAsync(balance: 500m, overdraftLimit: 1_000m);
        var destination = await Api.OpenAccountAsync();

        var response = await Api.TransferAsync(source.Id, destination.Id, 1_000m);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var account = (await Api.GetAccountAsync(source.Id)).Account;
        Assert.Equal(-500m, account.Balance);
        Assert.Equal(500m, account.OverdraftInUse);
        Assert.Equal(500m, account.AvailableToTransfer);
    }

    [Fact]
    public async Task Transfer_beyond_overdraft_is_rejected_and_recorded_as_failed()
    {
        var source = await Api.OpenAccountAsync(balance: 500m, overdraftLimit: 1_000m);
        var destination = await Api.OpenAccountAsync();

        var rejection = await ApiClient.ReadRejectionAsync(await Api.TransferAsync(source.Id, destination.Id, 1_600m));

        Assert.Equal(TransferStatus.Failed, rejection.Transfer.Status);
        Assert.Equal(TransferFailureReason.InsufficientFunds, rejection.Transfer.FailureReason);
        Assert.Equal(TransferStatus.Failed, (await Api.GetTransferAsync(rejection.Transfer.Id)).Status);
        Assert.Equal(500m, (await Api.GetAccountAsync(source.Id)).Account.Balance);
        Assert.Equal(1, (await Api.GetAccountAsync(source.Id)).Limits.Usage.AttemptsInLastHour);
    }

    [Fact]
    public async Task Receiving_while_in_overdraft_covers_it_first()
    {
        var debtor = await Api.OpenAccountAsync(overdraftLimit: 1_000m);
        var payer = await Api.OpenAccountAsync(balance: 5_000m);
        var sink = await Api.OpenAccountAsync();
        await Api.TransferAsync(debtor.Id, sink.Id, 800m);

        await Api.TransferAsync(payer.Id, debtor.Id, 1_000m);

        var account = (await Api.GetAccountAsync(debtor.Id)).Account;
        Assert.Equal(200m, account.Balance);
        Assert.Equal(0m, account.OverdraftInUse);
    }

    [Fact]
    public async Task Transfer_above_hourly_amount_limit_is_rejected()
    {
        var source = await Api.OpenAccountAsync(balance: 20_000m);
        var destination = await Api.OpenAccountAsync();
        await Api.TransferAsync(source.Id, destination.Id, 4_000m);

        var rejection = await ApiClient.ReadRejectionAsync(await Api.TransferAsync(source.Id, destination.Id, 1_500m));

        Assert.Equal(TransferFailureReason.HourlyAmountLimitExceeded, rejection.Transfer.FailureReason);
        Assert.Equal(HttpStatusCode.Created, (await Api.TransferAsync(source.Id, destination.Id, 1_000m)).StatusCode);
    }

    [Fact]
    public async Task Hourly_amount_window_slides_after_one_hour()
    {
        var source = await Api.OpenAccountAsync(balance: 20_000m);
        var destination = await Api.OpenAccountAsync();
        await Api.TransferAsync(source.Id, destination.Id, 5_000m);

        Factory.Clock.Advance(TimeSpan.FromMinutes(61));

        Assert.Equal(HttpStatusCode.Created, (await Api.TransferAsync(source.Id, destination.Id, 5_000m)).StatusCode);
    }

    [Fact]
    public async Task Rejected_transfers_count_towards_the_attempt_limit()
    {
        var source = await Api.OpenAccountAsync(balance: 100m);
        var destination = await Api.OpenAccountAsync();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await ApiClient.ReadRejectionAsync(await Api.TransferAsync(source.Id, destination.Id, 1_000m));
        }

        var rejection = await ApiClient.ReadRejectionAsync(await Api.TransferAsync(source.Id, destination.Id, 10m));

        Assert.Equal(TransferFailureReason.HourlyAttemptLimitExceeded, rejection.Transfer.FailureReason);
        Assert.Equal(0, (await Api.GetAccountAsync(source.Id)).Limits.Usage.RemainingAttempts);
    }

    [Fact]
    public async Task Nighttime_transfer_uses_nighttime_limits()
    {
        Factory.Clock.SetUtcNow(new DateTimeOffset(2026, 11, 12, 23, 0, 0, TimeSpan.FromHours(-3)).ToUniversalTime());
        var source = await Api.OpenAccountAsync(balance: 10_000m);
        var destination = await Api.OpenAccountAsync();

        var rejection = await ApiClient.ReadRejectionAsync(await Api.TransferAsync(source.Id, destination.Id, 1_500m));
        var accepted = await Api.TransferAsync(source.Id, destination.Id, 1_000m);
        var details = await Api.GetAccountAsync(source.Id);

        Assert.Equal(TransferFailureReason.HourlyAmountLimitExceeded, rejection.Transfer.FailureReason);
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        Assert.Equal(TransferPeriod.Nighttime, details.Limits.CurrentPeriod);
        Assert.Equal(1, details.Limits.Usage.RemainingAttempts);
    }

    [Fact]
    public async Task Daytime_transfers_still_count_against_the_nighttime_limit_after_22h()
    {
        Factory.Clock.SetUtcNow(new DateTimeOffset(2026, 11, 12, 21, 30, 0, TimeSpan.FromHours(-3)).ToUniversalTime());
        var source = await Api.OpenAccountAsync(balance: 10_000m);
        var destination = await Api.OpenAccountAsync();
        var daytime = await Api.TransferAsync(source.Id, destination.Id, 3_000m);

        Factory.Clock.Advance(TimeSpan.FromMinutes(40));
        var rejection = await ApiClient.ReadRejectionAsync(await Api.TransferAsync(source.Id, destination.Id, 10m));
        Factory.Clock.Advance(TimeSpan.FromMinutes(21));
        var afterWindow = await Api.TransferAsync(source.Id, destination.Id, 10m);

        Assert.Equal(HttpStatusCode.Created, daytime.StatusCode);
        Assert.Equal(TransferFailureReason.HourlyAmountLimitExceeded, rejection.Transfer.FailureReason);
        Assert.Equal(HttpStatusCode.Created, afterWindow.StatusCode);
    }

    [Fact]
    public async Task Blocked_accounts_cannot_send_or_receive()
    {
        var active = await Api.OpenAccountAsync(balance: 1_000m);
        var blocked = await Api.OpenAccountAsync(balance: 1_000m);
        await Api.Http.PostAsync($"/api/accounts/{blocked.Id}/block", null);

        var sending = await ApiClient.ReadRejectionAsync(await Api.TransferAsync(blocked.Id, active.Id, 10m));
        var receiving = await ApiClient.ReadRejectionAsync(await Api.TransferAsync(active.Id, blocked.Id, 10m));

        Assert.Equal(TransferFailureReason.SourceAccountNotActive, sending.Transfer.FailureReason);
        Assert.Equal(TransferFailureReason.DestinationAccountNotActive, receiving.Transfer.FailureReason);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1.001)]
    public async Task Invalid_amount_is_a_bad_request(decimal amount)
    {
        var source = await Api.OpenAccountAsync(balance: 100m);
        var destination = await Api.OpenAccountAsync();

        var response = await Api.TransferAsync(source.Id, destination.Id, amount);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Same_source_and_destination_is_a_bad_request()
    {
        var account = await Api.OpenAccountAsync(balance: 100m);

        var response = await Api.TransferAsync(account.Id, account.Id, 10m);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_account_is_not_found()
    {
        var source = await Api.OpenAccountAsync(balance: 100m);

        var response = await Api.TransferAsync(source.Id, Guid.NewGuid(), 10m);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
