using Transfers.Application.Common;
using Transfers.Domain.Transfers;

namespace Transfers.UnitTests.Application;

public sealed class DisplayFormatterTests
{
    private readonly DisplayFormatter _formatter = new(new TransferPeriodSchedule(
        new TimeOnly(6, 0),
        new TimeOnly(22, 0),
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")));

    [Theory]
    [InlineData(1500, "R$ 1.500,00")]
    [InlineData(-500, "-R$ 500,00")]
    [InlineData(0.5, "R$ 0,50")]
    public void Formats_money_in_brazilian_reais(decimal amount, string expected) =>
        Assert.Equal(expected, DisplayFormatter.Money(amount).Replace(' ', ' '));

    [Fact]
    public void Formats_instants_in_the_bank_time_zone() =>
        Assert.Equal("12/11/2026 12:00", _formatter.DateTime(new DateTimeOffset(2026, 11, 12, 15, 0, 0, TimeSpan.Zero)));

    [Fact]
    public void Formats_documents() => Assert.Equal("526.018.159-06", DisplayFormatter.Document("52601815906"));
}
