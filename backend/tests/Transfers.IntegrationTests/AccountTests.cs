using System.Net;
using System.Net.Http.Json;
using Transfers.Application.Accounts;
using Transfers.Domain.Accounts;
using Transfers.IntegrationTests.Infrastructure;

namespace Transfers.IntegrationTests;

public sealed class AccountTests(PostgresFixture postgres) : IntegrationTest(postgres)
{
    [Fact]
    public async Task Account_details_expose_limits_and_formatted_values()
    {
        var account = await Api.OpenAccountAsync(balance: 1_500m, overdraftLimit: 1_000m, name: "Ana Souza");

        var details = await Api.GetAccountAsync(account.Id);

        Assert.Equal("Ana Souza", details.Account.HolderName);
        Assert.Equal(2_500m, details.Account.AvailableToTransfer);
        Assert.Equal("R$ 2.500,00", details.Account.AvailableToTransferDisplay.Replace(' ', ' '));
        Assert.Equal(5_000m, details.Limits.Daytime.MaxAmount);
        Assert.Equal(3, details.Limits.Nighttime.MaxAttempts);
        Assert.Equal(5, details.Limits.Usage.RemainingAttempts);
    }

    [Fact]
    public async Task Duplicate_document_is_a_conflict()
    {
        var payload = new { holderName = "Ana", holderDocument = "52601815906", initialBalance = 0m, overdraftLimit = 0m };

        await Api.Http.PostAsJsonAsync("/api/accounts", payload);
        var response = await Api.Http.PostAsJsonAsync("/api/accounts", payload);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Overdraft_limit_is_configurable()
    {
        var account = await Api.OpenAccountAsync(balance: 100m);

        var response = await Api.Http.PutAsJsonAsync($"/api/accounts/{account.Id}/overdraft-limit", new { overdraftLimit = 900m });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1_000m, (await Api.GetAccountAsync(account.Id)).Account.AvailableToTransfer);
    }

    [Fact]
    public async Task Hourly_transfer_limits_are_configurable()
    {
        var source = await Api.OpenAccountAsync(balance: 20_000m);
        var destination = await Api.OpenAccountAsync();
        var rejected = await ApiClient.ReadRejectionAsync(await Api.TransferAsync(source.Id, destination.Id, 6_000m));

        var response = await Api.Http.PutAsJsonAsync($"/api/accounts/{source.Id}/transfer-limits", new
        {
            daytimeLimit = new { maxAmount = 10_000m, maxAttempts = 8 },
            nighttimeLimit = new { maxAmount = 2_000m, maxAttempts = 4 }
        });
        var details = await response.Content.ReadFromJsonAsync<AccountDetailsResponse>(ApiClient.Json);
        var accepted = await Api.TransferAsync(source.Id, destination.Id, 6_000m);

        Assert.Equal(Transfers.Domain.Transfers.TransferFailureReason.HourlyAmountLimitExceeded, rejected.Transfer.FailureReason);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(10_000m, details!.Limits.Daytime.MaxAmount);
        Assert.Equal(8, details.Limits.Daytime.MaxAttempts);
        Assert.Equal(2_000m, details.Limits.Nighttime.MaxAmount);
        Assert.Equal(4, details.Limits.Nighttime.MaxAttempts);
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
    }

    [Fact]
    public async Task Invalid_transfer_limits_are_a_bad_request()
    {
        var account = await Api.OpenAccountAsync();

        var zeroAmount = await Api.Http.PutAsJsonAsync($"/api/accounts/{account.Id}/transfer-limits", new
        {
            daytimeLimit = new { maxAmount = 0m, maxAttempts = 5 },
            nighttimeLimit = new { maxAmount = 1_000m, maxAttempts = 3 }
        });
        var missingPeriod = await Api.Http.PutAsJsonAsync($"/api/accounts/{account.Id}/transfer-limits", new
        {
            daytimeLimit = new { maxAmount = 1_000m, maxAttempts = 5 }
        });

        Assert.Equal(HttpStatusCode.BadRequest, zeroAmount.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingPeriod.StatusCode);
        Assert.Equal(5_000m, (await Api.GetAccountAsync(account.Id)).Limits.Daytime.MaxAmount);
    }

    [Fact]
    public async Task Status_actions_toggle_the_account()
    {
        var account = await Api.OpenAccountAsync();

        var blocked = await (await Api.Http.PostAsync($"/api/accounts/{account.Id}/block", null)).Content.ReadFromJsonAsync<AccountResponse>(ApiClient.Json);
        var again = await Api.Http.PostAsync($"/api/accounts/{account.Id}/block", null);
        var reactivated = await (await Api.Http.PostAsync($"/api/accounts/{account.Id}/activate", null)).Content.ReadFromJsonAsync<AccountResponse>(ApiClient.Json);

        Assert.Equal(AccountStatus.Blocked, blocked!.Status);
        Assert.True(blocked.Actions.CanActivate);
        Assert.False(blocked.Actions.CanBlock);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(AccountStatus.Active, reactivated!.Status);
    }

    [Fact]
    public async Task Unknown_account_is_not_found()
    {
        var response = await Api.Http.GetAsync($"/api/accounts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
