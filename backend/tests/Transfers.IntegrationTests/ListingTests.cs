using Transfers.Application.Accounts;
using Transfers.Application.Common;
using Transfers.Application.Transfers;
using Transfers.IntegrationTests.Infrastructure;

namespace Transfers.IntegrationTests;

public sealed class ListingTests(PostgresFixture postgres) : IntegrationTest(postgres)
{
    [Theory]
    [InlineData("HolderName")]
    [InlineData("HolderDocument")]
    [InlineData("Status")]
    [InlineData("Balance")]
    [InlineData("OverdraftLimit")]
    [InlineData("AvailableToTransfer")]
    [InlineData("OpenedAt")]
    public async Task Accounts_can_be_sorted_by_every_column(string column)
    {
        await SeedAccountsAsync();

        var ascending = await Api.GetPageAsync<AccountResponse>($"/api/accounts?sortBy={column}&sortDirection=Asc&pageSize=100");
        var descending = await Api.GetPageAsync<AccountResponse>($"/api/accounts?sortBy={column}&sortDirection=Desc&pageSize=100");

        Assert.Equal(column, ascending.SortBy);
        Assert.Equal(SortDirection.Desc, descending.SortDirection);
        Assert.Equal(ascending.TotalItems, descending.TotalItems);
        Assert.Equal(KeyOf(ascending.Items[0], column), KeyOf(descending.Items[^1], column));
    }

    [Fact]
    public async Task Accounts_sorted_by_balance_are_ordered_on_the_server_across_pages()
    {
        await SeedAccountsAsync();

        var firstPage = await Api.GetPageAsync<AccountResponse>("/api/accounts?sortBy=Balance&sortDirection=Desc&page=1&pageSize=2");
        var secondPage = await Api.GetPageAsync<AccountResponse>("/api/accounts?sortBy=Balance&sortDirection=Desc&page=2&pageSize=2");

        Assert.Equal([3_000m, 2_000m], firstPage.Items.Select(account => account.Balance));
        Assert.Equal([1_000m], secondPage.Items.Select(account => account.Balance));
        Assert.Equal(2, firstPage.TotalPages);
    }

    [Fact]
    public async Task Accounts_can_be_searched_by_name_or_document()
    {
        var ana = await Api.OpenAccountAsync(name: "Ana Pesquisa 7");
        await Api.OpenAccountAsync(name: "Bruno 7");

        var byName = await Api.GetPageAsync<AccountResponse>("/api/accounts?search=pesquisa%207");
        var byDocument = await Api.GetPageAsync<AccountResponse>($"/api/accounts?search={Uri.EscapeDataString(ana.HolderDocument)}");

        Assert.Equal([ana.Id], byName.Items.Select(account => account.Id));
        Assert.Equal([ana.Id], byDocument.Items.Select(account => account.Id));
    }

    [Theory]
    [InlineData("CreatedAt")]
    [InlineData("Kind")]
    [InlineData("Source")]
    [InlineData("Destination")]
    [InlineData("Amount")]
    [InlineData("ScheduledFor")]
    [InlineData("Status")]
    public async Task Transfers_can_be_sorted_by_every_column(string column)
    {
        var (ana, _, _) = await SeedTransfersAsync();

        var ascending = await Api.GetPageAsync<TransferResponse>($"/api/transfers?accountId={ana}&sortBy={column}&sortDirection=Asc");
        var descending = await Api.GetPageAsync<TransferResponse>($"/api/transfers?accountId={ana}&sortBy={column}&sortDirection=Desc");

        Assert.Equal(3, ascending.TotalItems);
        Assert.Equal(ascending.Items.Select(transfer => transfer.Id).Order(), descending.Items.Select(transfer => transfer.Id).Order());
        Assert.Equal(column, descending.SortBy);
    }

    [Fact]
    public async Task Transfers_sorted_by_amount_and_filtered_by_account()
    {
        var (ana, bruno, _) = await SeedTransfersAsync();

        var page = await Api.GetPageAsync<TransferResponse>($"/api/transfers?accountId={bruno}&sortBy=Amount&sortDirection=Asc");

        Assert.Equal([100m, 300m], page.Items.Select(transfer => transfer.Amount));
        Assert.All(page.Items, transfer => Assert.True(transfer.Source.AccountId == bruno || transfer.Destination.AccountId == bruno));
        Assert.Contains(page.Items, transfer => transfer.Source.AccountId == ana);
    }

    private async Task SeedAccountsAsync()
    {
        await Api.OpenAccountAsync(balance: 2_000m, name: "Bruno");
        await Api.OpenAccountAsync(balance: 3_000m, overdraftLimit: 100m, name: "Ana");
        var blocked = await Api.OpenAccountAsync(balance: 1_000m, overdraftLimit: 900m, name: "Carla");
        await Api.Http.PostAsync($"/api/accounts/{blocked.Id}/block", null);
    }

    private async Task<(Guid Ana, Guid Bruno, Guid Carla)> SeedTransfersAsync()
    {
        var ana = await Api.OpenAccountAsync(balance: 5_000m, name: "Ana");
        var bruno = await Api.OpenAccountAsync(balance: 5_000m, name: "Bruno");
        var carla = await Api.OpenAccountAsync(balance: 5_000m, name: "Carla");

        await Api.TransferAsync(ana.Id, bruno.Id, 300m);
        Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        await Api.TransferAsync(carla.Id, ana.Id, 200m);
        Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        await Api.ScheduleAsync(bruno.Id, ana.Id, 100m, Factory.Clock.GetUtcNow().AddDays(1));

        return (ana.Id, bruno.Id, carla.Id);
    }

    private static object KeyOf(AccountResponse account, string column) => column switch
    {
        "HolderName" => account.HolderName,
        "HolderDocument" => account.HolderDocument,
        "Status" => account.Status,
        "Balance" => account.Balance,
        "OverdraftLimit" => account.OverdraftLimit,
        "AvailableToTransfer" => account.AvailableToTransfer,
        _ => account.OpenedAt
    };
}
