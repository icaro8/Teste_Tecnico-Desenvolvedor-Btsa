using Transfers.Domain.Transfers;

namespace Transfers.UnitTests.Domain;

public sealed class FundsTransferServiceTests
{
    [Fact]
    public void Transfer_with_sufficient_balance_moves_money_atomically()
    {
        var source = TransferScenario.AccountWithBalance(1_000m);
        var destination = TransferScenario.AccountWithBalance(0m);

        var transfer = TransferScenario.Settle(source, destination, 400m);

        Assert.Equal(TransferStatus.Completed, transfer.Status);
        Assert.Equal(600m, source.Balance);
        Assert.Equal(400m, destination.Balance);
        Assert.Contains(transfer.DomainEvents, domainEvent => domainEvent is TransferCompleted);
    }

    [Fact]
    public void Transfer_can_use_the_overdraft()
    {
        var source = TransferScenario.AccountWithBalance(500m, overdraftLimit: 1_000m);
        var destination = TransferScenario.AccountWithBalance(0m);

        var transfer = TransferScenario.Settle(source, destination, 1_000m);

        Assert.Equal(TransferStatus.Completed, transfer.Status);
        Assert.Equal(-500m, source.Balance);
        Assert.Equal(500m, source.OverdraftInUse);
    }

    [Fact]
    public void Transfer_can_use_balance_plus_overdraft_entirely()
    {
        var source = TransferScenario.AccountWithBalance(500m, overdraftLimit: 1_000m);
        var destination = TransferScenario.AccountWithBalance(0m);

        var transfer = TransferScenario.Settle(source, destination, 1_500m);

        Assert.Equal(TransferStatus.Completed, transfer.Status);
        Assert.Equal(-1_000m, source.Balance);
    }

    [Fact]
    public void Transfer_beyond_the_overdraft_fails_without_moving_money()
    {
        var source = TransferScenario.AccountWithBalance(500m, overdraftLimit: 1_000m);
        var destination = TransferScenario.AccountWithBalance(0m);

        var transfer = TransferScenario.Settle(source, destination, 1_500.01m);

        Assert.Equal(TransferStatus.Failed, transfer.Status);
        Assert.Equal(TransferFailureReason.InsufficientFunds, transfer.FailureReason);
        Assert.Equal(500m, source.Balance);
        Assert.Equal(0m, destination.Balance);
    }

    [Fact]
    public void Money_received_while_in_overdraft_covers_it_first()
    {
        var payer = TransferScenario.AccountWithBalance(2_000m);
        var receiver = TransferScenario.AccountWithBalance(-800m, overdraftLimit: 1_000m);

        TransferScenario.Settle(payer, receiver, 1_000m);

        Assert.Equal(200m, receiver.Balance);
        Assert.Equal(0m, receiver.OverdraftInUse);
        Assert.Equal(1_200m, receiver.AvailableToTransfer);
    }

    [Fact]
    public void Transfer_above_the_hourly_amount_limit_fails()
    {
        var source = TransferScenario.AccountWithBalance(10_000m);
        var destination = TransferScenario.AccountWithBalance(0m);

        var transfer = TransferScenario.Settle(source, destination, 1_000.01m, new HourlyTransferUsage(1, 4_000m));

        Assert.Equal(TransferFailureReason.HourlyAmountLimitExceeded, transfer.FailureReason);
        Assert.Equal(10_000m, source.Balance);
    }

    [Fact]
    public void Transfer_up_to_the_hourly_amount_limit_completes()
    {
        var source = TransferScenario.AccountWithBalance(10_000m);
        var destination = TransferScenario.AccountWithBalance(0m);

        var transfer = TransferScenario.Settle(source, destination, 1_000m, new HourlyTransferUsage(1, 4_000m));

        Assert.Equal(TransferStatus.Completed, transfer.Status);
    }

    [Fact]
    public void Exceeding_the_hourly_attempts_fails_even_with_funds()
    {
        var source = TransferScenario.AccountWithBalance(10_000m);
        var destination = TransferScenario.AccountWithBalance(0m);

        var transfer = TransferScenario.Settle(source, destination, 10m, new HourlyTransferUsage(5, 0m));

        Assert.Equal(TransferFailureReason.HourlyAttemptLimitExceeded, transfer.FailureReason);
    }

    [Fact]
    public void Nighttime_transfers_use_the_nighttime_limits()
    {
        var source = TransferScenario.AccountWithBalance(10_000m);
        var destination = TransferScenario.AccountWithBalance(0m);

        var night = TransferScenario.Settle(source, destination, 1_500m, period: TransferPeriod.Nighttime);
        var day = TransferScenario.Settle(source, destination, 1_500m, period: TransferPeriod.Daytime);

        Assert.Equal(TransferFailureReason.HourlyAmountLimitExceeded, night.FailureReason);
        Assert.Equal(TransferStatus.Completed, day.Status);
    }

    [Fact]
    public void Nighttime_attempt_limit_is_lower()
    {
        var source = TransferScenario.AccountWithBalance(10_000m);
        var destination = TransferScenario.AccountWithBalance(0m);

        var transfer = TransferScenario.Settle(source, destination, 10m, new HourlyTransferUsage(3, 0m), TransferPeriod.Nighttime);

        Assert.Equal(TransferFailureReason.HourlyAttemptLimitExceeded, transfer.FailureReason);
    }

    [Fact]
    public void Blocked_source_cannot_send()
    {
        var source = TransferScenario.AccountWithBalance(1_000m);
        var destination = TransferScenario.AccountWithBalance(0m);
        source.Block();

        var transfer = TransferScenario.Settle(source, destination, 100m);

        Assert.Equal(TransferFailureReason.SourceAccountNotActive, transfer.FailureReason);
    }

    [Fact]
    public void Inactive_destination_cannot_receive()
    {
        var source = TransferScenario.AccountWithBalance(1_000m);
        var destination = TransferScenario.AccountWithBalance(0m);
        destination.Deactivate();

        var transfer = TransferScenario.Settle(source, destination, 100m);

        Assert.Equal(TransferFailureReason.DestinationAccountNotActive, transfer.FailureReason);
        Assert.Equal(1_000m, source.Balance);
    }

    [Fact]
    public void Scheduling_requires_both_accounts_to_be_active()
    {
        var active = TransferScenario.AccountWithBalance(1_000m);
        var blocked = TransferScenario.AccountWithBalance(1_000m);
        var inactive = TransferScenario.AccountWithBalance(0m);
        blocked.Block();
        inactive.Deactivate();

        Assert.Null(FundsTransferService.CheckCanSchedule(active, TransferScenario.AccountWithBalance(0m)));
        Assert.Equal(TransferErrors.SourceAccountNotActive, FundsTransferService.CheckCanSchedule(blocked, active));
        Assert.Equal(TransferErrors.DestinationAccountNotActive, FundsTransferService.CheckCanSchedule(active, inactive));
    }
}
