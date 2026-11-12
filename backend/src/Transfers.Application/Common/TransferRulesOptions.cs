namespace Transfers.Application.Common;

public sealed class TransferRulesOptions
{
    public const string SectionName = "TransferRules";

    public string TimeZone { get; init; } = "America/Sao_Paulo";

    public TimeOnly DaytimeStart { get; init; } = new(6, 0);

    public TimeOnly NighttimeStart { get; init; } = new(22, 0);

    public HourlyLimitOptions DefaultDaytimeLimit { get; init; } = new() { MaxAmount = 5_000m, MaxAttempts = 5 };

    public HourlyLimitOptions DefaultNighttimeLimit { get; init; } = new() { MaxAmount = 1_000m, MaxAttempts = 3 };
}

public sealed class HourlyLimitOptions
{
    public decimal MaxAmount { get; init; }

    public int MaxAttempts { get; init; }
}
