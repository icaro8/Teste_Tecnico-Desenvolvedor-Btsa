using Transfers.Domain.Accounts;

namespace Transfers.UnitTests.Domain;

public sealed class AccountTests
{
    [Theory]
    [InlineData("")]
    [InlineData("123.456.789-00")]
    [InlineData("111.111.111-11")]
    [InlineData("1234")]
    public void Open_rejects_invalid_documents(string document)
    {
        var result = Account.Open("Ana", document, 0m, 0m, AccountFactory.Limits(), AccountFactory.Now);

        Assert.Equal(AccountErrors.InvalidDocument, result.Error);
    }

    [Fact]
    public void Open_normalizes_formatted_document_and_starts_active()
    {
        var account = Account.Open(" Ana Souza ", "526.018.159-06", 500m, 1_000m, AccountFactory.Limits(), AccountFactory.Now).Value;

        Assert.Equal("52601815906", account.HolderDocument);
        Assert.Equal("Ana Souza", account.HolderName);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Equal(1_500m, account.AvailableToTransfer);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10.555)]
    public void Open_rejects_invalid_overdraft_limit(decimal overdraftLimit)
    {
        var result = Account.Open("Ana", "52601815906", 0m, overdraftLimit, AccountFactory.Limits(), AccountFactory.Now);

        Assert.Equal(AccountErrors.InvalidOverdraftLimit, result.Error);
    }

    [Fact]
    public void Overdraft_limit_cannot_drop_below_the_overdraft_in_use()
    {
        var account = TransferScenario.AccountWithBalance(-800m, overdraftLimit: 1_000m);

        var result = account.ChangeOverdraftLimit(500m);

        Assert.Equal(AccountErrors.OverdraftLimitBelowUsage, result.Error);
        Assert.Equal(1_000m, account.OverdraftLimit);
    }

    [Fact]
    public void Transfer_limits_can_be_reconfigured()
    {
        var account = AccountFactory.Open();
        var limits = AccountFactory.Limits(dayAmount: 8_000m, dayAttempts: 10, nightAmount: 500m, nightAttempts: 2);

        account.ChangeTransferLimits(limits);

        Assert.Equal(8_000m, account.Limits.Daytime.MaxAmount);
        Assert.Equal(2, account.Limits.Nighttime.MaxAttempts);
    }

    [Fact]
    public void Overdraft_limit_can_be_reconfigured()
    {
        var account = AccountFactory.Open(balance: 100m, overdraftLimit: 0m);

        var result = account.ChangeOverdraftLimit(2_000m);

        Assert.True(result.IsSuccess);
        Assert.Equal(2_100m, account.AvailableToTransfer);
    }

    [Fact]
    public void Status_changes_follow_the_lifecycle()
    {
        var account = AccountFactory.Open();

        Assert.True(account.Block().IsSuccess);
        Assert.Equal(AccountErrors.AlreadyInStatus, account.Block().Error);
        Assert.True(account.Deactivate().IsSuccess);
        Assert.True(account.Activate().IsSuccess);
        Assert.True(account.IsActive);
    }
}
