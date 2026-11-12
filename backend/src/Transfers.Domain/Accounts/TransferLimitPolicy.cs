using Transfers.Domain.Transfers;

namespace Transfers.Domain.Accounts;

public sealed record TransferLimitPolicy
{
    private TransferLimitPolicy()
    {
        Daytime = null!;
        Nighttime = null!;
    }

    public TransferLimitPolicy(HourlyLimit daytime, HourlyLimit nighttime)
    {
        Daytime = daytime;
        Nighttime = nighttime;
    }

    public HourlyLimit Daytime { get; private init; }

    public HourlyLimit Nighttime { get; private init; }

    public HourlyLimit For(TransferPeriod period) => period switch
    {
        TransferPeriod.Daytime => Daytime,
        TransferPeriod.Nighttime => Nighttime,
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, null)
    };
}
