using System.Net;
using Transfers.Domain.Transfers;
using Transfers.IntegrationTests.Infrastructure;

namespace Transfers.IntegrationTests;

public sealed class ScheduledTransferTests(PostgresFixture postgres) : IntegrationTest(postgres)
{
    [Fact]
    public async Task Scheduled_transfer_runs_when_due()
    {
        var source = await Api.OpenAccountAsync(balance: 1_000m);
        var destination = await Api.OpenAccountAsync();

        var response = await Api.ScheduleAsync(source.Id, destination.Id, 400m, Factory.Clock.GetUtcNow().AddHours(2));
        var scheduled = await ApiClient.ReadTransferAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(TransferStatus.Scheduled, scheduled.Status);
        Assert.True(scheduled.CanCancel);
        Assert.Equal(0, await Factory.ProcessDueTransfersAsync());

        Factory.Clock.Advance(TimeSpan.FromHours(2));
        Assert.Equal(1, await Factory.ProcessDueTransfersAsync());

        var executed = await Api.GetTransferAsync(scheduled.Id);
        Assert.Equal(TransferStatus.Completed, executed.Status);
        Assert.False(executed.CanCancel);
        Assert.Equal(600m, (await Api.GetAccountAsync(source.Id)).Account.Balance);
    }

    [Fact]
    public async Task Scheduled_transfer_without_funds_at_execution_fails()
    {
        var source = await Api.OpenAccountAsync(balance: 1_000m);
        var destination = await Api.OpenAccountAsync();
        var scheduled = await ApiClient.ReadTransferAsync(
            await Api.ScheduleAsync(source.Id, destination.Id, 900m, Factory.Clock.GetUtcNow().AddDays(1)));

        await Api.TransferAsync(source.Id, destination.Id, 800m);
        Factory.Clock.Advance(TimeSpan.FromDays(1));
        await Factory.ProcessDueTransfersAsync();

        var executed = await Api.GetTransferAsync(scheduled.Id);
        Assert.Equal(TransferStatus.Failed, executed.Status);
        Assert.Equal(TransferFailureReason.InsufficientFunds, executed.FailureReason);
        Assert.Equal(200m, (await Api.GetAccountAsync(source.Id)).Account.Balance);
    }

    [Fact]
    public async Task Scheduled_transfer_rechecks_hourly_limits_at_execution()
    {
        var source = await Api.OpenAccountAsync(balance: 20_000m);
        var destination = await Api.OpenAccountAsync();
        var executionTime = Factory.Clock.GetUtcNow().AddMinutes(30);
        var scheduled = await ApiClient.ReadTransferAsync(await Api.ScheduleAsync(source.Id, destination.Id, 2_000m, executionTime));

        Factory.Clock.Advance(TimeSpan.FromMinutes(10));
        await Api.TransferAsync(source.Id, destination.Id, 4_000m);
        Factory.Clock.Advance(TimeSpan.FromMinutes(20));
        await Factory.ProcessDueTransfersAsync();

        Assert.Equal(TransferFailureReason.HourlyAmountLimitExceeded, (await Api.GetTransferAsync(scheduled.Id)).FailureReason);
    }

    [Fact]
    public async Task Scheduling_with_an_inactive_account_is_rejected_upfront()
    {
        var active = await Api.OpenAccountAsync(balance: 1_000m);
        var blocked = await Api.OpenAccountAsync(balance: 1_000m);
        var inactive = await Api.OpenAccountAsync(balance: 1_000m);
        await Api.Http.PostAsync($"/api/accounts/{blocked.Id}/block", null);
        await Api.Http.PostAsync($"/api/accounts/{inactive.Id}/deactivate", null);
        var tomorrow = Factory.Clock.GetUtcNow().AddDays(1);

        var fromBlocked = await Api.ScheduleAsync(blocked.Id, active.Id, 10m, tomorrow);
        var toInactive = await Api.ScheduleAsync(active.Id, inactive.Id, 10m, tomorrow);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, fromBlocked.StatusCode);
        Assert.Contains("transfer.source_account_not_active", await fromBlocked.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, toInactive.StatusCode);
        Assert.Contains("transfer.destination_account_not_active", await toInactive.Content.ReadAsStringAsync());
        var page = await Api.GetPageAsync<Transfers.Application.Transfers.TransferResponse>($"/api/transfers?accountId={active.Id}");
        Assert.Equal(0, page.TotalItems);
    }

    [Fact]
    public async Task Account_blocked_after_scheduling_makes_the_execution_fail()
    {
        var source = await Api.OpenAccountAsync(balance: 1_000m);
        var destination = await Api.OpenAccountAsync();
        var scheduled = await ApiClient.ReadTransferAsync(
            await Api.ScheduleAsync(source.Id, destination.Id, 100m, Factory.Clock.GetUtcNow().AddHours(1)));

        await Api.Http.PostAsync($"/api/accounts/{destination.Id}/block", null);
        Factory.Clock.Advance(TimeSpan.FromHours(1));
        await Factory.ProcessDueTransfersAsync();

        var executed = await Api.GetTransferAsync(scheduled.Id);
        Assert.Equal(TransferStatus.Failed, executed.Status);
        Assert.Equal(TransferFailureReason.DestinationAccountNotActive, executed.FailureReason);
        Assert.Equal(1_000m, (await Api.GetAccountAsync(source.Id)).Account.Balance);
    }

    [Fact]
    public async Task Cancelled_schedule_is_never_executed()
    {
        var source = await Api.OpenAccountAsync(balance: 1_000m);
        var destination = await Api.OpenAccountAsync();
        var scheduled = await ApiClient.ReadTransferAsync(
            await Api.ScheduleAsync(source.Id, destination.Id, 100m, Factory.Clock.GetUtcNow().AddHours(1)));

        var cancel = await Api.Http.PostAsync($"/api/transfers/{scheduled.Id}/cancel", null);
        var cancelled = await ApiClient.ReadTransferAsync(cancel);
        Factory.Clock.Advance(TimeSpan.FromHours(2));
        await Factory.ProcessDueTransfersAsync();

        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal(TransferStatus.Cancelled, cancelled.Status);
        Assert.Equal(TransferStatus.Cancelled, (await Api.GetTransferAsync(scheduled.Id)).Status);
        Assert.Equal(1_000m, (await Api.GetAccountAsync(source.Id)).Account.Balance);
        Assert.Equal(HttpStatusCode.Conflict, (await Api.Http.PostAsync($"/api/transfers/{scheduled.Id}/cancel", null)).StatusCode);
    }

    [Fact]
    public async Task Completed_transfer_cannot_be_cancelled()
    {
        var source = await Api.OpenAccountAsync(balance: 1_000m);
        var destination = await Api.OpenAccountAsync();
        var transfer = await ApiClient.ReadTransferAsync(await Api.TransferAsync(source.Id, destination.Id, 10m));

        var response = await Api.Http.PostAsync($"/api/transfers/{transfer.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Schedule_in_the_past_is_a_bad_request()
    {
        var source = await Api.OpenAccountAsync(balance: 1_000m);
        var destination = await Api.OpenAccountAsync();

        var response = await Api.ScheduleAsync(source.Id, destination.Id, 10m, Factory.Clock.GetUtcNow().AddMinutes(-1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_transfer_is_not_found()
    {
        var response = await Api.Http.GetAsync($"/api/transfers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
