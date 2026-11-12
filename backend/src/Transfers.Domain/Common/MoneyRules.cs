namespace Transfers.Domain.Common;

public static class MoneyRules
{
    public const int DecimalPlaces = 2;

    public static bool HasValidPrecision(decimal amount) => decimal.Round(amount, DecimalPlaces) == amount;
}
