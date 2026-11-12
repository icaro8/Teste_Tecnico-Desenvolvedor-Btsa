using Transfers.Domain.Transfers;

namespace Transfers.UnitTests.Domain;

public sealed class TransferTests
{
    private static readonly Guid Source = Guid.NewGuid();
    private static readonly Guid Destination = Guid.NewGuid();
    private static readonly DateTimeOffset Now = AccountFactory.Now;

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(10.001)]
    public void Amount_must_be_positive_with_cents_precision(decimal amount)
    {
        var result = Transfer.CreateImmediate(Source, Destination, amount, null, Now);

        Assert.Equal(TransferErrors.InvalidAmount, result.Error);
    }

    [Fact]
    public void Source_and_destination_must_differ()
    {
        var result = Transfer.CreateImmediate(Source, Source, 10m, null, Now);

        Assert.Equal(TransferErrors.SameAccount, result.Error);
    }

    [Fact]
    public void Schedule_must_be_in_the_future()
    {
        var result = Transfer.Schedule(Source, Destination, 10m, Now, null, Now);

        Assert.Equal(TransferErrors.ScheduleMustBeInFuture, result.Error);
    }

    [Fact]
    public void Scheduling_normalizes_to_utc_and_raises_event()
    {
        var local = new DateTimeOffset(2026, 11, 13, 9, 0, 0, TimeSpan.FromHours(-3));

        var transfer = Transfer.Schedule(Source, Destination, 10m, local, null, Now).Value;

        Assert.Equal(TransferStatus.Scheduled, transfer.Status);
        Assert.Equal(TimeSpan.Zero, transfer.ScheduledFor!.Value.Offset);
        Assert.Equal(local, transfer.ScheduledFor);
        Assert.Contains(transfer.DomainEvents, domainEvent => domainEvent is TransferScheduled);
    }

    [Fact]
    public void Scheduled_transfer_can_be_cancelled_once()
    {
        var transfer = Transfer.Schedule(Source, Destination, 10m, Now.AddDays(1), null, Now).Value;

        Assert.True(transfer.Cancel(Now).IsSuccess);
        Assert.Equal(TransferStatus.Cancelled, transfer.Status);
        Assert.Equal(TransferErrors.NotCancellable, transfer.Cancel(Now).Error);
    }

    [Fact]
    public void Immediate_transfer_cannot_be_cancelled()
    {
        var transfer = Transfer.CreateImmediate(Source, Destination, 10m, null, Now).Value;

        Assert.Equal(TransferErrors.NotCancellable, transfer.Cancel(Now).Error);
    }

    [Fact]
    public void Scheduled_transfer_cannot_start_before_it_is_due()
    {
        var transfer = Transfer.Schedule(Source, Destination, 10m, Now.AddHours(1), null, Now).Value;

        Assert.Throws<InvalidOperationException>(() => transfer.StartProcessing(Now));
    }

    [Fact]
    public void Due_scheduled_transfer_moves_to_processing()
    {
        var transfer = Transfer.Schedule(Source, Destination, 10m, Now.AddHours(1), null, Now).Value;

        transfer.StartProcessing(Now.AddHours(1));

        Assert.Equal(TransferStatus.Processing, transfer.Status);
        Assert.Equal(Now.AddHours(1), transfer.ProcessedAt);
    }

    [Fact]
    public void Idempotent_terms_compare_every_relevant_field()
    {
        var transfer = Transfer.Schedule(Source, Destination, 10m, Now.AddHours(1), "key", Now).Value;

        Assert.True(transfer.HasSameTerms(Source, Destination, 10m, Now.AddHours(1)));
        Assert.False(transfer.HasSameTerms(Source, Destination, 11m, Now.AddHours(1)));
    }
}
