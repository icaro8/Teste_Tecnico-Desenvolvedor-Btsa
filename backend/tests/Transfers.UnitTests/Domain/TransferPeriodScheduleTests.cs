using Transfers.Domain.Transfers;

namespace Transfers.UnitTests.Domain;

public sealed class TransferPeriodScheduleTests
{
    private static readonly TimeSpan SaoPauloOffset = TimeSpan.FromHours(-3);

    private readonly TransferPeriodSchedule _schedule = new(
        new TimeOnly(6, 0),
        new TimeOnly(22, 0),
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"));

    [Theory]
    [InlineData(5, 59, TransferPeriod.Nighttime)]
    [InlineData(6, 0, TransferPeriod.Daytime)]
    [InlineData(13, 30, TransferPeriod.Daytime)]
    [InlineData(21, 59, TransferPeriod.Daytime)]
    [InlineData(22, 0, TransferPeriod.Nighttime)]
    [InlineData(0, 0, TransferPeriod.Nighttime)]
    public void Resolves_period_in_the_configured_time_zone(int hour, int minute, TransferPeriod expected)
    {
        var instant = new DateTimeOffset(2026, 11, 12, hour, minute, 0, SaoPauloOffset).ToUniversalTime();

        Assert.Equal(expected, _schedule.Resolve(instant));
    }
}
