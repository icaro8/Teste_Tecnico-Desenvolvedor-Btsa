namespace Transfers.Domain.Transfers;

public sealed class TransferPeriodSchedule
{
    public TransferPeriodSchedule(TimeOnly daytimeStart, TimeOnly nighttimeStart, TimeZoneInfo timeZone)
    {
        if (daytimeStart == nighttimeStart)
        {
            throw new ArgumentException("Daytime and nighttime must start at different times.", nameof(nighttimeStart));
        }

        DaytimeStart = daytimeStart;
        NighttimeStart = nighttimeStart;
        TimeZone = timeZone;
    }

    public TimeOnly DaytimeStart { get; }

    public TimeOnly NighttimeStart { get; }

    public TimeZoneInfo TimeZone { get; }

    public TransferPeriod Resolve(DateTimeOffset instant)
    {
        var localTime = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, TimeZone).DateTime);
        return localTime.IsBetween(DaytimeStart, NighttimeStart)
            ? TransferPeriod.Daytime
            : TransferPeriod.Nighttime;
    }
}
