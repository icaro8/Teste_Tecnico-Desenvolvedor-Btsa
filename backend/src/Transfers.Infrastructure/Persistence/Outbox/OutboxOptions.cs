namespace Transfers.Infrastructure.Persistence.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public bool Enabled { get; init; } = true;

    public int PollingIntervalMilliseconds { get; init; } = 1_000;

    public int BatchSize { get; init; } = 50;

    /// <summary>Depois de tantas falhas a mensagem para de ser tentada e fica para análise (dead letter).</summary>
    public int MaxAttempts { get; init; } = 5;
}
