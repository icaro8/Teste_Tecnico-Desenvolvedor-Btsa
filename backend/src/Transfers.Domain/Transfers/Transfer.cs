using Transfers.Domain.Common;

namespace Transfers.Domain.Transfers;

public sealed class Transfer : AggregateRoot
{
    private Transfer()
    {
    }

    private Transfer(
        Guid sourceAccountId,
        Guid destinationAccountId,
        decimal amount,
        TransferKind kind,
        TransferStatus status,
        DateTimeOffset? scheduledFor,
        string? idempotencyKey,
        DateTimeOffset now)
    {
        Id = Guid.CreateVersion7(now);
        SourceAccountId = sourceAccountId;
        DestinationAccountId = destinationAccountId;
        Amount = amount;
        Kind = kind;
        Status = status;
        ScheduledFor = scheduledFor;
        IdempotencyKey = idempotencyKey;
        CreatedAt = now;
    }

    public Guid SourceAccountId { get; private set; }

    public Guid DestinationAccountId { get; private set; }

    public decimal Amount { get; private set; }

    public TransferKind Kind { get; private set; }

    public TransferStatus Status { get; private set; }

    public TransferFailureReason? FailureReason { get; private set; }

    public string? IdempotencyKey { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ScheduledFor { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? FailedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public bool CanBeCancelled => Status == TransferStatus.Scheduled;

    public static Result<Transfer> CreateImmediate(
        Guid sourceAccountId,
        Guid destinationAccountId,
        decimal amount,
        string? idempotencyKey,
        DateTimeOffset now)
    {
        var error = Validate(sourceAccountId, destinationAccountId, amount);
        if (error is not null)
        {
            return error;
        }

        return new Transfer(sourceAccountId, destinationAccountId, amount, TransferKind.Immediate, TransferStatus.Processing, null, idempotencyKey, now)
        {
            ProcessedAt = now
        };
    }

    public static Result<Transfer> Schedule(
        Guid sourceAccountId,
        Guid destinationAccountId,
        decimal amount,
        DateTimeOffset scheduledFor,
        string? idempotencyKey,
        DateTimeOffset now)
    {
        var error = Validate(sourceAccountId, destinationAccountId, amount);
        if (error is not null)
        {
            return error;
        }

        if (scheduledFor <= now)
        {
            return TransferErrors.ScheduleMustBeInFuture;
        }

        var executionTime = scheduledFor.ToUniversalTime();
        var transfer = new Transfer(sourceAccountId, destinationAccountId, amount, TransferKind.Scheduled, TransferStatus.Scheduled, executionTime, idempotencyKey, now);
        transfer.Raise(new TransferScheduled(transfer.Id, executionTime, now));
        return transfer;
    }

    public bool IsDue(DateTimeOffset now) => Status == TransferStatus.Scheduled && ScheduledFor <= now;

    public bool HasSameTerms(Guid sourceAccountId, Guid destinationAccountId, decimal amount, DateTimeOffset? scheduledFor) =>
        SourceAccountId == sourceAccountId
        && DestinationAccountId == destinationAccountId
        && Amount == amount
        && ScheduledFor == scheduledFor?.ToUniversalTime();

    public void StartProcessing(DateTimeOffset now)
    {
        EnsureStatus(TransferStatus.Scheduled);
        if (!IsDue(now))
        {
            throw new InvalidOperationException($"Transfer {Id} is not due yet.");
        }

        Status = TransferStatus.Processing;
        ProcessedAt = now;
    }

    public Result Cancel(DateTimeOffset now)
    {
        if (!CanBeCancelled)
        {
            return TransferErrors.NotCancellable;
        }

        Status = TransferStatus.Cancelled;
        CancelledAt = now;
        Raise(new TransferCancelled(Id, now));
        return Result.Success();
    }

    internal void Complete(DateTimeOffset now)
    {
        EnsureStatus(TransferStatus.Processing);
        Status = TransferStatus.Completed;
        CompletedAt = now;
        Raise(new TransferCompleted(Id, SourceAccountId, DestinationAccountId, Amount, now));
    }

    internal void Fail(TransferFailureReason reason, DateTimeOffset now)
    {
        EnsureStatus(TransferStatus.Processing);
        Status = TransferStatus.Failed;
        FailureReason = reason;
        FailedAt = now;
        Raise(new TransferFailed(Id, reason, now));
    }

    private static Error? Validate(Guid sourceAccountId, Guid destinationAccountId, decimal amount)
    {
        if (amount <= 0 || !MoneyRules.HasValidPrecision(amount))
        {
            return TransferErrors.InvalidAmount;
        }

        return sourceAccountId == destinationAccountId ? TransferErrors.SameAccount : null;
    }

    private void EnsureStatus(TransferStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException($"Transfer {Id} is {Status}, expected {expected}.");
        }
    }
}
