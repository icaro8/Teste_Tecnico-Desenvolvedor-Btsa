namespace Transfers.IntegrationTests.Infrastructure;

internal static class CpfGenerator
{
    public static IEnumerable<string> Take(int count)
    {
        var random = new Random(42);
        for (var generated = 0; generated < count; generated++)
        {
            var digits = Enumerable.Range(0, 9).Select(_ => random.Next(10)).ToList();
            digits.Add(CheckDigit(digits));
            digits.Add(CheckDigit(digits));
            yield return string.Concat(digits);
        }
    }

    private static int CheckDigit(List<int> digits)
    {
        var weight = digits.Count + 1;
        var sum = digits.Select((digit, index) => digit * (weight - index)).Sum();
        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
