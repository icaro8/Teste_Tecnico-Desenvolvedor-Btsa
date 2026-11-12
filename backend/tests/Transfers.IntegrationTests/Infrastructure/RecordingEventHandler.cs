using System.Collections.Concurrent;
using Transfers.Application.Abstractions;
using Transfers.Domain.Common;
using Transfers.Domain.Transfers;

namespace Transfers.IntegrationTests.Infrastructure;

/// <summary>Handler de teste que registra os eventos entregues pelo outbox e pode simular falhas.</summary>
public sealed class RecordingEventHandler :
    IDomainEventHandler<TransferCompleted>,
    IDomainEventHandler<TransferCancelled>
{
    private int _failuresToSimulate;

    public ConcurrentQueue<IDomainEvent> Delivered { get; } = new();

    public void FailNextDeliveries(int count) => Interlocked.Exchange(ref _failuresToSimulate, count);

    public Task HandleAsync(TransferCompleted domainEvent, CancellationToken cancellationToken) => Record(domainEvent);

    public Task HandleAsync(TransferCancelled domainEvent, CancellationToken cancellationToken) => Record(domainEvent);

    private Task Record(IDomainEvent domainEvent)
    {
        if (Interlocked.Decrement(ref _failuresToSimulate) >= 0)
        {
            throw new InvalidOperationException("Falha simulada na entrega do evento.");
        }

        Delivered.Enqueue(domainEvent);
        return Task.CompletedTask;
    }
}
