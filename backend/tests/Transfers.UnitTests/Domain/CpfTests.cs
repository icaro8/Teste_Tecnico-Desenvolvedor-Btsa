using Transfers.Domain.Accounts;

namespace Transfers.UnitTests.Domain;

public sealed class CpfTests
{
    [Theory]
    [InlineData("526.018.159-06", "52601815906")]
    [InlineData("08301661305", "08301661305")]
    public void Normalizes_valid_numbers(string input, string expected) => Assert.Equal(expected, Cpf.Normalize(input));

    [Theory]
    [InlineData(null)]
    [InlineData("526.018.159-07")]
    [InlineData("000.000.000-00")]
    [InlineData("abc")]
    public void Rejects_invalid_numbers(string? input) => Assert.Null(Cpf.Normalize(input));
}
