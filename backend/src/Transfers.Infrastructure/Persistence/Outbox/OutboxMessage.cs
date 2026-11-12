using Transfers.Domain.Common;

namespace Transfers.Infrastructure.Persistence.Outbox;

/// <summary>
/// Evento de domínio gravado na mesma transação da mudança de estado que o gerou
/// (transactional outbox). Um worker o entrega depois, com garantia at-least-once.
/// </summary>
internal sealed class OutboxMessage
{
    public const int MaxErrorLength = 2_000;

    private OutboxMessage()
    {
        Type = string.Empty;
        Payload = string.Empty;
    }

    private OutboxMessage(string type, string payload, DateTimeOffset occurredAt)
    {
        Id = Guid.CreateVersion7(occurredAt);
        Type = type;
        Payload = payload;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; }

    public string Payload { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public int Attempts { get; private set; }

    public string? LastError { get; private set; }

    public static OutboxMessage From(IDomainEvent domainEvent) =>
        new(OutboxSerializer.TypeNameOf(domainEvent), OutboxSerializer.Serialize(domainEvent), domainEvent.OccurredAt);

    public void MarkProcessed(DateTimeOffset now)
    {
        Attempts++;
        ProcessedAt = now;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        Attempts++;
        LastError = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
    }
}
